export const API_URL = "http://140.137.41.136:1380/IPBMS/OinkAPI/api"; // API 路徑
export const VHidden = "visually-hidden";

// 等待提示元素
const waittingElement = `
  <div class="h-100 w-100 p-5" id="waitting">
    <div class="d-flex justify-content-center align-items-center text-center h-100">
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
export const addParam = (paramKey, paramValue) => {
  try {
    const urlObj = new URL(window.location.href); // 取得當前頁面 URL
    urlObj.searchParams.set(paramKey, paramValue); // 添加、更新參數
    window.history.replaceState(null, '', urlObj.toString()); // 更新當前頁面的 URL 而不刷新頁面
  } catch (error) { console.error(`URL添加參數時發生錯誤，原因如下: ${error}`); }
};

// 接收多個view，只顯示第一個，其於則隱藏
export const toggleView = (...views) => views.forEach((view, i) => view.classList.toggle(VHidden, i !== 0));

// 添加等待元素
export const addWaitting = (element) => {
  if (!element.querySelector('#waitting'))
    element.insertAdjacentHTML('beforeend', waittingElement);
}

// 移除 element 中所有指定 tag 元素
export const removeElement = (element, tag) => element.querySelectorAll(tag).forEach(e => e.remove());

// 執行服務並取得recordId
export const excuteService = async (url) => (await getResponse(url)).recordId;

export async function getResponse(url) {
  try {
    const response = await fetch(url, { method: "GET" });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`錯誤：${response.status} - ${errorText}`);
    }
    const result = await response.json();
    return result;
  } catch (error) {
    console.error("發生錯誤：", error);
    throw error; // 將錯誤拋出以供呼叫者處理
  }
}

// 圖片展示元素來源設置
export const setImage = (element, url, type) => {
  try { element.src !== url && (element.src = url); }
  catch (error) {
    element.src = '';
    console.error(`取得${type}時發生錯誤:`, error);
    return;
  }
}

// 影片展示元素來源設置
export const setVideo = (element, url, type) => {
  try {
    if (!element) throw new Error("元素未定義或不存在");

    const sourceElement = element.querySelector("source");
    if (!sourceElement) throw new Error("無法找到 source 元素");
    sourceElement.src !== url && (sourceElement.src = url);
    element.style.display = 'block';
    element.load(); // 重新加載影片來源
  }
  catch (error) { console.error(`取得${type}時發生錯誤:`, error); return; }
}

export const display = (element) => {
  element.classList.remove('d-none');
  element.classList.remove('visually-hidden');
  element.classList.add('d-block');
  element.style.display = 'block';
}
export const hidden = (element) => {
  element.classList.remove('d-block');
  element.classList.add('d-none');
  element.classList.add('visually-hidden');
  element.style.display = 'none';
}