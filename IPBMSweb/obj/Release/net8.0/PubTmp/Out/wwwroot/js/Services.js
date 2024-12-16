var ServiceName, errorMsg, Excute, Exhibit; // 服務名稱、錯誤文字元素、以及定義兩個用於設置對應服務的執行與展示函數
var MediaType, MediaList, RecordList; // 對應服務輸入的媒體類型及清單，以及該服務記錄清單
var source, target, result, prefix, suffix; // 網頁必要元素項

import { setSharedElements, onSharedElementsUpdated } from './sharedElement.js';
import { API_URL } from './lib/serviceLib.js';
import * as ES from './Estimate.js';
import * as TS from './Tracking.js';

// 初始化並載入頁面元素
export async function initialize(serviceName, views, msg) {
  ServiceName = serviceName;
  errorMsg = msg;

  // 依據服務設置函數
  switch (ServiceName) {
    case "ESTIMATE":
      Excute = ES.EstimateExcute;
      Exhibit = ES.EstimateExhibit;
      MediaType = 'IMAGE';
      MediaList = await ES.getEstimateMediaList();
      RecordList = await ES.getEstimateRecordList();
      break;
    case "TRACKING":
      Excute = TS.TrackingExcute;
      Exhibit = TS.TrackingExhibit;
      MediaType = 'VIDEO';
      MediaList = await TS.getTrackingMediaList();
      RecordList = await TS.getTrackingRecordList();
      break;
    default: console.error("服務類型未設置，無法設置方法!!!");
  }

  // 設置變數
  if (MediaType) setSharedElements(errorMsg, views);

  showInitialDetail(); // 展示初始化設置資訊
}

// 顯示初始化設置，bool參數決定是否顯示詳細內容
function showInitialDetail(detail = null) {
  const initialElements = { source, target, result, prefix, suffix, Excute, Exhibit };
  let resultMsg = Object.entries(initialElements)
    .map(([key, value]) => `${key} ${value ? (detail && value ? `: ${typeof value === 'function' ? value.name : value}` : '已設置') : '未設置'}`)
    .join('\n');
  console.log(`${ServiceName} Mode.\n` + resultMsg.toString());
}

// 等待初始化變數設置回應
onSharedElementsUpdated((sharedElements) => {
  // console.log("Shared elements updated:", sharedElements);
  ({ source, target, result, prefix, suffix } = sharedElements);
});

// 記錄參數
export function RecordParam() {
  if (ServiceName) {
    const recordId = new URLSearchParams(window.location.search).get('recordId'); // 獲取URL參數
    if (recordId) {
      console.log(`準備讀取編號 ${recordId} 辨識記錄!`)
      Exhibit(recordId); // 如果 recordId 存在則執行函數
    } else setConditionQuery(); // 若無記錄則執行前置條件清單設置
  } else { console.error('服務尚未初始化，無法載入記錄!'); }
}

// 服務執行與展示決策
export async function ServiceModeExcute() {
  if (ServiceName) {
    // 選擇(待辨識媒體)與查詢(辨識記錄)兩模式
    const mode = prefix.querySelector(".mode");
    if (!mode) { console.error("條件選擇的類型未選擇或不存在!"); return; }
    // 取得並檢測當前模式選項內容
    const selectedId = mode.querySelector(".select-box")?.dataset.selected;
    if (!selectedId) { console.error("未選擇有效的項目！"); return; }
    // 初始化時以自動設定該服務的執行與展示方法，透過模式去決定執行何項
    switch (mode.id) {
      case 'select': await Excute(selectedId); break;
      case 'search': await Exhibit(selectedId); break;
      default: console.error("條件選擇的類型未選擇或不存在!");
    }
  } else { console.error('服務尚未初始化，無法執行服務!') }
}

// 設置前置條件清單
export function setConditionQuery() {
  console.log('exc')
  const mode = prefix.querySelector(".mode");
  if (!mode) { console.error("條件選擇的類型未選擇或不存在!"); return; }
  const modeId = mode.id;

  let dataList, optionsHTML;
  if (modeId === 'select' && MediaList) { dataList = MediaList; }
  else if (modeId === 'search' && RecordList) { dataList = RecordList; }
  else { console.error("條件選擇的類型未選擇或不存在!"); return; }

  optionsHTML = dataList
    .map(item => createOptionHTML(item, modeId === 'select', MediaType === 'IMAGE'))
    .join('');
  setOptionList(modeId, optionsHTML);
}

// 建立選單選項
function createOptionHTML(item, isSelectMod = false, isImage = false) {
  const isImageSelectMod = isSelectMod && isImage
  const MID = item.id;
  const OptionInfo = isSelectMod ? item.fileName : item.timestamp;
  const MediaURL = isImageSelectMod ? `${API_URL}/Media/Image/${MID}` : null;
  return `
    <div class="option-item" data-value="${MID}">
      ${isImageSelectMod ? `<img src="${MediaURL}">` : ""}
      <span>${OptionInfo}</span>
    </div>
  `;
}

// 設置選項清單
function setOptionList(mode, optionsHTML) {
  const optionList = prefix.querySelector(`#${mode} .select-options`);
  if (optionList) { optionList.innerHTML = optionsHTML; }
  else { console.error("選項列表元素不存在!"); }
}