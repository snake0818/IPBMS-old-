import { onSharedElementsUpdated } from './sharedElement.js';
import * as sLib from './lib/serviceLib.js'
import * as subPage from './lib/subPage.js'

// ********** 設置變數 ********** //
var errMsg, source, target, result, prefix, suffix;

onSharedElementsUpdated((sharedElements) => {
  // console.log("Shared elements updated:", sharedElements);
  ({ errMsg, source, target, result, prefix, suffix } = sharedElements);
});

// ********** 豬隻身長估測服務 ********** //

export async function getEstimateMediaList() { return await sLib.getResponse(`${sLib.API_URL}/Media/Image/List`); }
export async function getEstimateRecordList() { return await sLib.getResponse(`${sLib.API_URL}/Estimate/List`); }

// 圖片估測服務流程
export async function EstimateExcute(imageId) {
  errMsg.innerHTML = '';
  try {
    switchAndWaittingResults();

    const recordId = await sLib.excuteService(`${sLib.API_URL}/Estimate/${imageId}`);
    sLib.addParam('recordId', recordId); // 賦予記錄
    if (!recordId) {
      const ERRORMSG = "估測服務失敗";
      errMsg.innerText = ERRORMSG;
      throw new Error(ERRORMSG);
    }

    await EstimateExhibit(recordId, true);
  } catch (error) {
    console.error("豬隻大小估測服務執行時發生錯誤:", error);
    errMsg.innerHTML = error;
    sLib.removeElement(suffix, "#waitting");
    sLib.toggleView(prefix, suffix);
  }
}

// 取得並展示原始資訊與結果紀錄
export async function EstimateExhibit(recordId, afterExcute = null) {
  const resultImgElement = target.querySelector("#resultPreview1");
  const depthMapElement = target.querySelector("#resultPreview2");
  errMsg.innerHTML = '';
  try {
    if (!afterExcute) {
      sLib.addParam('recordId', recordId); // 賦予記錄
      switchAndWaittingResults();
    }
    const record = await sLib.getResponse(`${sLib.API_URL}/Estimate/Record/${recordId}`);
    const imageId = record.imageId;

    // // 檢查並設置原始檔案展示元素
    // sLib.setImage(source, `${sLib.API_URL}/Media/Image/${imageId}`, "原始圖片");
    // sLib.removeElement(source, "#waitting");
    // sLib.display(source);

    sLib.setImage(resultImgElement, `${sLib.API_URL}/Estimate/Image/${recordId}`, "估測結果圖片");
    sLib.removeElement(resultImgElement, "#waitting");
    // sLib.display(resultImgElement);

    sLib.setImage(depthMapElement, `${sLib.API_URL}/Estimate/DepthMap/${recordId}`, "估測結果深度圖");
    sLib.removeElement(depthMapElement, "#waitting");
    // sLib.display(depthMapElement);

    renderFormatRecordData(recordId);

    // const subRecords = record.PigRecords;
    // if (subRecords) creatPigPerviewList(subRecords, result); // 取得並展示各豬隻圖片與資訊
  } catch (error) {
    console.error("豬隻大小估測記錄展示時發生錯誤:", error);
    errMsg.innerHTML = error;
    // sLib.hidden(source);
    sLib.hidden(resultImgElement);
    sLib.hidden(depthMapElement);
    sLib.toggleView(prefix, suffix);
  }
  finally {
    sLib.removeElement(suffix, "#waitting");
  }
}

const switchAndWaittingResults = () => {
  // 切換結果展示介面，並添加等待元素
  sLib.toggleView(suffix, prefix);
  sLib.addWaitting(suffix);
  // 各展示元素添加等待回應
  // sLib.addWaitting(target);
  // sLib.addWaitting(source.parentNode);
  // sLib.addWaitting(result.parentNode.parentNode);
}

// ********** 方法 ********** //

// // 建立豬隻估測紀錄清單
// function creatPigPerviewList(RecordArr, result) {
//   result.innerHTML = ''; // 清空 result

//   RecordArr.forEach((PigId, index) => {
//     sLib.removeElement(result.parentNode.parentNode, "#waitting");

//     const imgSrc = `${sLib.API_URL}/Pig/Image/${PigId}`;

//     const cardItemHTML = `
//     <li class="card-item shadow">
//       <div class="img-container">
//         <img src="${imgSrc}">
//       </div>
//       <div class="card-body text-center p-0 pt-2">
//         <h4 class="card-title fw-bold m-0">Pig ${PigId}</h4>
//       </div>
//       <button class="card-btn" value="${PigId}"></button>
//     </li>
//     `;

//     // 將卡片物件元素插入 result 中
//     result.insertAdjacentHTML('beforeend', cardItemHTML);
//   });

//   const btns = result.querySelectorAll('.card-btn');
//   btns.forEach(btn => {
//     btn.onclick = async () => { subPage.openSubPage(await renderPigInfo(btn.value)); }
//   });
// }

// // 建立豬隻的圖片與資料區塊，並插入到 result
// async function renderPigInfo(PigId) {
//   const imgSrc = `${sLib.API_URL}/Pig/Image/${PigId}`;
//   const imgAlt = `Pig ${PigId}`;
//   const infoData = await formatPigData(PigId); // 取得資訊內容

//   // 創建卡片物件元素
//   const cardItemHTML = `
//     <div class="row info-card">
//       <div class="col-lg-auto img-container">
//         <img src="${imgSrc}" alt="${imgAlt}">
//       </div>
//       <div class="col-lg-auto info-container">
//         <h3 class="info-title">Pig ID: ${PigId}</h3>
//         <div class="info-content">${infoData}</div>
//       </div>
//     </div>
//   `;

//   return cardItemHTML;
// }

// // 將 API 資料格式化
// async function formatPigData(Aid) {
//   try {
//     // 發送 GET 請求取得 Pig 資料
//     const response = await fetch(`${sLib.API_URL}/Pig/Data/${Aid}`);
//     if (!response.ok) { throw new Error(`無法取得 Pig ID ${Aid} 的資料：${response.statusText}`); }
//     const data = await response.json();

//     // 格式化取得的資料
//     return data.map(item => {
//       const keypoints = item.keypoints
//         .map(point => `<p>- ${point.name}: (x: ${point.x.toFixed(2)}, y: ${point.y.toFixed(2)})</p>`)
//         .join('\n');
//       return `
//         <h5 class="info-sub-title">Bounding Box:</h5>
//         <p>寬: ${item.bounding_box.xmax}, 高: ${item.bounding_box.ymax} (單位: Pixel)<p>
//         <h5 class="info-sub-title">信心度: <span>${item.confidence.toFixed(2) * 100}%</span></h5>
//         <h5 class="info-sub-title">關鍵點:</h5>
//         ${keypoints}
//       `.trim();
//     }).join('\n\n');
//   } catch (error) {
//     console.error("取得資料時發生錯誤：", error);
//     throw error;
//   }
// }

// 將 API 資料格式化
async function renderFormatRecordData(Rid) {
  try {
    // 發送 GET 請求取得 Pig 資料
    const response = await fetch(`${sLib.API_URL}/Estimate/Data/${Rid}`);
    if (!response.ok) { throw new Error(`無法取得記錄編號 ${Rid} 的資料：${response.statusText}`); }
    const data = await response.json();

    // 檢查 keypoints 是否存在並格式化資料
    const keypoints = data.keypoints?.length
      ? data.keypoints
        .map(point => {
          const coordinates = Object.entries(point)
            .filter(([key]) => key === 'x' || key === 'y')
            .map(([key, value]) => `<div>${key}: <span>${value.toFixed(2)}<span></div>`)
            .join('\n');
          return `
              <h3>${point.name}: </h3>
              <div class="keypoint">
                ${coordinates}
              </div>
            `
        })
        .join('\n')
      : '<p>無可用的關鍵點資料</p>';

    // 格式化輸出的 HTML
    const recordInfo = `
      <div class="record-container">
        <h1 class="info-title">記錄編號: ${Rid}</h1>
        <h2 class="info-sub-title">身長(cm): <span>${data.length?.toFixed(2) || '未知'}</span></h2>
        ${keypoints}
      </div>
    `.trim();

    result.innerHTML = recordInfo; // 使用 innerHTML 更新內容
  } catch (error) {
    console.error("取得資料時發生錯誤：", error);
    throw error;
  }
}