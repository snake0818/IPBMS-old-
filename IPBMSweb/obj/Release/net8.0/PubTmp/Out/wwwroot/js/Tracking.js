import { onSharedElementsUpdated } from './sharedElement.js';
import * as sLib from './lib/serviceLib.js';

// ********** 設置變數 ********** //
var errMsg, source, target, result, prefix, suffix;
onSharedElementsUpdated((sharedElements) => {
  // console.log("Shared elements updated:", sharedElements);
  ({ errMsg, source, target, result, prefix, suffix } = sharedElements);
});

// ********** 豬隻追蹤辨識服務 ********** //
// 豬場影像、追蹤記錄清單
export async function getTrackingMediaList() { return await sLib.getResponse(`${sLib.API_URL}/Media/Video/List`); }
export async function getTrackingRecordList() { return await sLib.getResponse(`${sLib.API_URL}/Tracking/List`); }

// 影片追蹤服務流程
export async function TrackingExcute(videoId) {
  errMsg.innerHTML = '';
  try {
    switchAndWaittingResults();

    // 執行追蹤服務，並啟動非同步串流影像與即時數據，等待回應記錄編號
    const recordId = await eventSSE(`${sLib.API_URL}/Tracking/${videoId}`);
    sLib.addParam('recordId', recordId); // 賦予記錄
    if (!recordId) {
      const ERRORMSG = "追蹤服務失敗";
      errMsg.innerText = ERRORMSG;
      throw new Error(ERRORMSG);
    }

    await TrackingExhibit(recordId, true);
  } catch (error) {
    console.error('豬隻活動追蹤服務執行時發生錯誤:', error);
    errMsg.innerHTML = error;
    sLib.removeElement(suffix, "#waitting");
    sLib.toggleView(prefix, suffix);
  }
}

// 取得並展示原始資訊與結果紀錄
export async function TrackingExhibit(recordId, afterExcute = null) {
  // 追蹤結果影片
  const resultVideoElement = target.querySelector('video');
  // 追蹤結果圖片
  const resultImgElement = result.querySelector('img');
  errMsg.innerHTML = '';

  try {
    if (!afterExcute) {
      sLib.addParam('recordId', recordId); // 賦予記錄
      switchAndWaittingResults();
    }
    
    const record = await sLib.getResponse(`${sLib.API_URL}/Tracking/Record/${recordId}`);
    const imageId = record.imageId;

    sLib.setVideo(resultVideoElement, `${sLib.API_URL}/Tracking/Video/${recordId}`, "追蹤結果影片");
    sLib.display(resultVideoElement);

    sLib.setImage(resultImgElement, `${sLib.API_URL}/Tracking/Image/${recordId}`, "追蹤結果圖片");
    sLib.display(resultImgElement);

    // 追蹤結果數據
    if (!afterExcute) {
      const resultDataContainer = suffix.querySelector('#resultInfo');
      const OverviewContainer = resultDataContainer.querySelector('#trackingOverview');
      const DataContainer = resultDataContainer.querySelector('#realTimeData .scrollable-container');
      const data = await sLib.getResponse(`${sLib.API_URL}/Tracking/Data/${recordId}`);
      updatedOverview(OverviewContainer, data);
      updateValue(OverviewContainer, "#RecordId .value-item", recordId);
      updatedData(DataContainer, data);
    }
  } catch (error) {
    console.error("豬隻活動追蹤記錄展示時發生錯誤:", error);
    errMsg.innerHTML = error;
    sLib.hidden(resultVideoElement);
    sLib.hidden(resultImgElement);
    sLib.toggleView(prefix, suffix);
  }
  finally {
    sLib.removeElement(suffix, "#waitting");
  }
}

const switchAndWaittingResults = () => {
  // 切換結果展示介面，並添加等待元素
  sLib.toggleView(suffix, prefix);
  // sLib.addWaitting(suffix);
  // 各展示元素添加等待回應
  sLib.addWaitting(target);
  sLib.addWaitting(result);
  const trackingInfoContainer = suffix.querySelector('#resultInfo');
  sLib.addWaitting(trackingInfoContainer);
}

// 格式化內容方法
const formatValue = (value) => (value === null || value === 0 ? "-" : value);
const updateValue = (container, selectElement, value, transform = (v) => v) => {
  if (value) container.querySelector(selectElement).innerText = transform(value);
};

// 更新總覽資訊方法
const updatedOverview = (overviewContainer, data) => {
  sLib.display(overviewContainer);
  updateValue(overviewContainer, "#Frames #Total", data.video_total_frame);
  updateValue(overviewContainer, "#DateTime .value-item", data.record_start_time, (t) => new Date(t).toISOString().replace("T", " ").replace("Z", "").split(".")[0]);
  updateValue(overviewContainer, "#ModelVersion .value-item", data.tracking_accuracy.model_version);
}
// 更新數據資訊方法
const updatedData = (dataContainer, data) => {
  if (Array.isArray(data.pigs)) {
    if (data.pigs.length > 0) {
      sLib.display(dataContainer.parentNode);
      data.pigs.slice() // 複製數據
        .sort((a, b) => a.id - b.id) // 排序
        .forEach(pig => { // 處理各豬隻數據
          const pigID = pig.id;
          const formatPigValues = { // 數據變數
            activity: pig.activity_tracking.activity_distance,
            eatingCount: pig.feeding_tracking.eat_count,
            totalEatingTime: pig.drinking_tracking.drink_count,
            drinkingCount: pig.corner_tracking.corner_count,
            totalDrinkingTime: pig.feeding_tracking.total_eat_duration,
            corneringCount: pig.drinking_tracking.total_drink_duration,
            totalCorneringTime: pig.corner_tracking.total_corner_duration
          };

          const pigContainer = dataContainer.querySelector(`#pig-${pigID}`);
          if (pigContainer) { // 更新已存在的豬隻數據
            Object.entries(formatPigValues)
              .forEach(([key, value]) => { pigContainer.querySelector(`.${key}`).innerText = formatValue(value); });
          } else { // 如果豬隻資料區不存在，建立一個
            const newPigContainer = document.createElement("div");
            newPigContainer.id = `pig-${pigID}`;
            newPigContainer.className = "pig-card";
            newPigContainer.innerHTML = `
          <div class="id">${pigID}</div>
          ${Object.entries(formatPigValues)
                .map(([key, value]) => `<div class="${key}">${formatValue(value)}</div>`)
                .join("")}
        `;
            dataContainer.appendChild(newPigContainer);

            // 排序
            Array.from(dataContainer.getElementsByClassName("pig-card"))
              .sort((a, b) => parseInt(a.querySelector(".id").textContent, 10) - parseInt(b.querySelector(".id").textContent, 10))
              .forEach(pigCard => dataContainer.appendChild(pigCard));
          }
        });
    }
  } else console.error("pigs is undefined or not an array");
}

// 啟動 SSE (Server-Sent Events)
async function eventSSE(url) {
  const overviewContainer = document.querySelector("#trackingOverview");
  const dataViewContainer = document.querySelector("#realTimeData");
  const dataContainer = dataViewContainer.querySelector(".scrollable-container");

  // 插入即時辨識影像串流元素
  sLib.removeElement(target, "#waitting");
  const streamURL = `${sLib.API_URL}/Tracking/Streaming`
  const streamElementHTML = `<img id="video-stream" src="${streamURL}" alt="即時影像串流" style="max-width: 100%; border: 1px solid #000;"></img>`;
  target.insertAdjacentHTML("beforeend", streamElementHTML);
  const streamElement = document.getElementById("video-stream");

  return new Promise((resolve, reject) => { // Promise 封裝了 EventSource 的操作
    const eventSource = new EventSource(url); // 建立 EventSource 用於處理伺服器發送的事件
    // 定義事件處理函數，當接收伺服器發送消息時觸發
    eventSource.onmessage = function (event) {
      const data = JSON.parse(event.data); // 解析即時訊息與數據

      if (data && Object.keys(data).length > 0) {
        // 更新當前數據幀
        if (data.frame_count) overviewContainer.querySelector("#Frames #Current").innerText = data.frame_count;

        if (data.start) { // 更新辨識總覽
          updatedOverview(overviewContainer, data);
        }
        else if (data.end) { // 主動結束 SSE 以避免錯誤發生，並且回應辨識記錄編號
          updateValue(overviewContainer, "#RecordId .value-item", data.record_id);
          console.log("任務完成，關閉 SSE 連接");
          if (streamElement && streamElement.parentNode) target.removeChild(streamElement); // 清除即時辨識影像串流元素
          eventSource.close(); // 關閉 SSE 連線
          resolve(data.recordId); // 將 `recordId` 傳回 Promise
        }
        else { // 更新即時數據
          const trackingInfoContainer = suffix.querySelector('#resultInfo');
          sLib.removeElement(trackingInfoContainer, '#waitting');
          sLib.display(overviewContainer);
          updatedData(dataContainer, data);
          updatedOverview(overviewContainer, data);
          updateValue(overviewContainer, "#AverageConfidence .value-item", data.tracking_accuracy.average_tracking_confidence, (v) => v.toFixed(2));
        }
      }
    };
    // 定義錯誤處理函數，當發生錯誤時觸發
    eventSource.onerror = function (error) {
      console.error(`SSE 發生錯誤: ${url}`, error);
      if (streamElement && streamElement.parentNode) target.removeChild(streamElement); // 清除即時辨識影像串流元素
      sLib.hidden(overviewContainer);
      sLib.hidden(dataViewContainer);
      eventSource.close(); // 關閉 SSE 連線
      reject(new Error("SSE 發生錯誤")); // 拒絕 Promise
    };
  });
}
