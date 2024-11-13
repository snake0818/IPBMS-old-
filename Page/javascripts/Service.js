var ServiceName, errorMsg, Excute, Exhibit;
var source, target, result, prefix, suffix;
// 初始化加載元素
export function initialize(serviceName, views, msg) {
  ServiceName = serviceName;
  errorMsg = msg;
  ({ source, target, result, prefix, suffix } = views);
  switch (ServiceName) { // 依據服務設置函數
    case "ESTIMATE":
      console.log("ESTIMATE Mode.");
      Excute = EstimateExcute;
      Exhibit = EstimateExhibit;
      break;
    case "TRACKING":
      console.log("TRACKING Mode.");
      Excute = TrackingExcute;
      Exhibit = TrackingExhibit;
      break;
    default: console.error("服務類型未設置，無法設置方法!!!");
  }
  checkInitial();
}

export function RecordParam() {
  if (ServiceName) {
    const recordId = new URLSearchParams(window.location.search).get('recordId'); // 獲取URL參數
    recordId && Exhibit(recordId); // 如果 recordId 存在則執行函數
  } else { console.error('服務尚未初始化，無法載入記錄!') }
}

export function ServiceModeExcute() {
  if (ServiceName) {
    const mode = prefix.querySelector(".mode");
    if (!mode) { console.error("條件選擇的類型未選擇或不存在!"); return; }

    const selectedId = mode.querySelector(".select-box")?.dataset.selected;
    if (!selectedId) { console.error("未選擇有效的項目！"); return; }

    switch (mode.id) {
      case 'select': Excute(selectedId); break;
      case 'search': Exhibit(selectedId); break;
      default: console.error("條件選擇的類型未選擇或不存在!");
    }
  } else { console.error('服務尚未初始化，無法執行服務!') }
}

function checkInitial(showDetail = null) {
  const initialElements = { source, target, result, prefix, suffix, Excute, Exhibit };
  let resultMsg = Object.entries(initialElements)
    .map(([key, value]) => `${key} ${value ? (showDetail && value ? `: ${typeof value === 'function' ? value.name : value}` : '存在') : '不存在'}`)
    .join('\n');
  console.log(resultMsg.toString());
}

async function GetRecord(URL, recordId) {
  // 取得紀錄
  const record = await sendRequest(URL, 'GET');
  if (!record) { errorMsg.innerText = "該紀錄不存在!"; return; }
  addParam('recordId', recordId); // 賦予記錄
  return record;
}

// ****************************** 功能 ****************************** //

const API_URL = "http://140.137.41.136:1380/IPBMS/OinkAPI/api"; // API 路徑
import * as subPage from './subPage.js'

// ********** 豬隻身長估測服務 ********** //
// 圖片估測服務流程
async function EstimateExcute(imageId) {
  try {
    WattingResultView(prefix, suffix);
    const recordId = await getService(`${API_URL}/Estimate/${imageId}`);
    if (!recordId) {
      const ERRORMSG = "估測服務失敗";
      errorMsg.innerText = ERRORMSG;
      throw new Error(ERRORMSG);
    }

    await EstimateExhibit(recordId, true); // 取得並展示紀錄
  } catch (error) { console.error("豬隻身長估測服務執行時發生錯誤:", error); }
  finally { removeElement(suffix, "#waitting"); }
}

// 取得並展示原始資訊與結果紀錄
async function EstimateExhibit(recordId, hasWaitting = null) {
  try {
    if (!hasWaitting) WattingResultView(prefix, suffix);

    const record = await GetRecord(`${API_URL}/Estimate/Record/${recordId}`, recordId);
    const imageId = record.imageId;
    const subRecords = record.annotationRecords;

    // 檢查並設置原始檔案展示元素
    setImage(source, `${API_URL}/Media/Image/${imageId}`, "原始圖片");
    setImage(target, `${API_URL}/Estimate/Record/Image/${recordId}`, "豬隻估測結果圖片");
    creatPigPerviewList(subRecords, result); // 取得並展示各豬隻圖片與資訊
  } catch (error) { console.error("豬隻身長估測記錄展示時發生錯誤:", error); }
  finally { if (!hasWaitting) removeElement(suffix, "#waitting"); }
}

// 建立豬隻估測紀錄清單
function creatPigPerviewList(RecordArr, result) {
  result.innerHTML = ''; // 清空 result

  RecordArr.forEach((AnnotationId, index) => {
    const imgSrc = `${API_URL}/Annotation/Image/${AnnotationId}`;

    const cardItemHTML = `
    <li class="card-item shadow">
      <div class="img-container">
        <img src="${imgSrc}">
      </div>
      <div class="card-body text-center p-0 pt-2">
        <h4 class="card-title fw-bold m-0">Annotation ${index + 1}</h4>
      </div>
      <button class="card-btn" value="${AnnotationId}"></button>
    </li>
    `;

    // 將卡片物件元素插入 result 中
    result.insertAdjacentHTML('beforeend', cardItemHTML);
  });

  const btns = result.querySelectorAll('.card-btn');
  btns.forEach(btn => {
    btn.onclick = async () => { subPage.openSubPage(await renderPigInfo(btn.value)); }
  });
}

// 建立豬隻的圖片與資料區塊，並插入到 result
async function renderPigInfo(AnnotationId) {
  const imgSrc = `${API_URL}/Annotation/Image/${AnnotationId}`;
  const imgAlt = `Pig anntotaion ${AnnotationId}`;
  const infoData = await formatPigData(AnnotationId); // 取得資訊內容

  // 創建卡片物件元素
  const cardItemHTML = `
    <div class="row info-card">
      <div class="col-lg-auto img-container">
        <img src="${imgSrc}" alt="Pig annotation ${imgAlt}">
      </div>
      <div class="col-lg-auto info-container">
        <h3 class="info-title">Annotation ID: ${AnnotationId}</h3>
        <div class="info-content">${infoData}</div>
      </div>
    </div>
  `;

  return cardItemHTML;
}

// 將 API 資料格式化
async function formatPigData(Aid) {
  try {
    // 發送 GET 請求取得 Annotation 資料
    const response = await fetch(`${API_URL}/Annotation/Data/${Aid}`);
    if (!response.ok) { throw new Error(`無法取得 Annotation ID ${Aid} 的資料：${response.statusText}`); }
    const data = await response.json();

    // 格式化取得的資料
    return data.map(item => {
      const keypoints = item.keypoints
        .map(point => `<p>- ${point.name}: (x: ${point.x.toFixed(2)}, y: ${point.y.toFixed(2)})</p>`)
        .join('\n');
      return `
        <h5 class="info-sub-title">Bounding Box:</h5>
        <p>寬: ${item.bounding_box.xmax}, 高: ${item.bounding_box.ymax} (單位: Pixel)<p>
        <h5 class="info-sub-title">信心度: <span>${item.confidence.toFixed(2) * 100}%</span></h5>
        <h5 class="info-sub-title">關鍵點:</h5>
        ${keypoints}
      `.trim();
    }).join('\n\n');
  } catch (error) {
    console.error("取得資料時發生錯誤：", error);
    throw error;
  }
}

// ********** 豬隻追蹤辨識服務 ********** //
// 影片追蹤檢測服務流程
async function TrackingExcute(videoId) {
  try {
    WattingResultView(prefix, suffix);
    const recordId = await getService(`${API_URL}/Tracking/${videoId}`);
    if (!recordId) {
      const ERRORMSG = "追蹤檢測服務失敗";
      errorMsg.innerText = ERRORMSG;
      throw new Error(ERRORMSG);
    }

    await TrackingExhibit(recordId, true);
  } catch (error) { console.error('追蹤檢測服務執行時發生錯誤:', error); }
  finally { removeElement(resultBlock, "#waitting"); }
}

// 取得並展示原始資訊與結果紀錄
async function TrackingExhibit(recordId, hasWaitting = null) {
  try {
    if (!hasWaitting) WattingResultView(prefix, suffix);
    const record = await GetRecord(`${API_URL}/Tracking/Record/${recordId}`, recordId);

    const resultVideoElement = target.querySelector('video');
    setVideo(resultVideoElement, `${API_URL}/Tracking/Record/Video/${recordId}`, "追蹤檢測結果影片");
    console.log(target, resultVideoElement);
    target.classList.remove(VHidden);

    const resultImgElement = result.querySelector('img');
    setImage(resultImgElement, `${API_URL}/Tracking/Record/Image/${recordId}`, "追蹤檢測結果圖片");
    result.classList.remove(VHidden);
  } catch (error) {
    target.classList.add(VHidden);
    result.classList.add(VHidden);
    console.error("豬隻追蹤檢測記錄展示時發生錯誤:", error);
  }
  finally { if (!hasWaitting) removeElement(suffix, "#waitting"); }
}

// ****************************** 方法 ****************************** //
const VHidden = "visually-hidden";
// 等待提示元素
const waittingElement = `
  <div class="h-100 w-100 p-5" id="waitting">
    <div class="d-flex justify-content-center align-items-center h-100">
      <div>
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <div class="fs-3">正在處理請求中...</div>
      </div>            
    </div>
  </div>
`;

// 服務頁面 URL 添加 recordId
const addParam = (paramKey, paramValue) => {
  try {
    const urlObj = new URL(window.location.href); // 取得當前頁面 URL
    urlObj.searchParams.set(paramKey, paramValue); // 添加、更新參數
    window.history.replaceState(null, '', urlObj.toString()); // 更新當前頁面的 URL 而不刷新頁面
  } catch (error) { console.error(`URL添加參數時發生錯誤，原因如下: ${error}`); }
};

// 接收多個view，但只顯示第一個，其於則隱藏
const toggleView = (...views) => views.forEach((view, i) => view.classList.toggle(VHidden, i !== 0));

// 切換結果展示區塊並插入等待提示元素
const WattingResultView = (pre, suf) => {
  toggleView(suf, pre);
  suf.insertAdjacentHTML('beforeend', waittingElement);
}

// 移除element中tag元素
const removeElement = (element, tag) => element.querySelector(tag)?.remove();

// 送出請求並取得回應
async function sendRequest(url, method, data = null) {
  try {
    const response = await fetch(url, (data ? { method: method, body: data, } : { method: method, }));
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`錯誤：${response.status} - ${errorText}`);
    }
    return await response.json();
  } catch (error) {
    console.error("發生錯誤：", error);
    return null;
    throw error; // 若需要，將錯誤丟出供呼叫者處理
  }
}

// 執行服務並取得recordId
async function getService(url) {
  try {
    const response = await fetch(url, { method: "GET" });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`錯誤：${response.status} - ${errorText}`);
    }
    const result = await response.json();
    const recordId = result.recordId;
    return recordId;
  } catch (error) {
    console.error("發生錯誤：", error);
    throw error; // 將錯誤拋出以供呼叫者處理
  }
}

// 圖片展示元素來源設置
const setImage = (element, url, type) => {
  console.log('i')
  try { element.src !== url && (element.src = url); }
  catch (error) {
    element.src = '';
    console.error(`取得${type}時發生錯誤:`, error);
    return;
  }
}

// 影片展示元素來源設置
const setVideo = (element, url, type) => {
  console.log('v')
  try {
    if (!element) throw new Error("元素未定義或不存在");

    const sourceElement = element.querySelector("source");
    if (!sourceElement) throw new Error("無法找到 source 元素");
    console.log(element, sourceElement)
    sourceElement.src !== url && (sourceElement.src = url);
    element.style.display = 'block';
    element.load(); // 重新加載影片來源
  }
  catch (error) { console.error(`取得${type}時發生錯誤:`, error); return; }
}