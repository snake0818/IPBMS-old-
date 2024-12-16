// 網頁必要元素項 errMsg, source, target, result, prefix, suffix;
export const sharedElements = {
  errMsg: null,
  source: null,
  target: null,
  result: null,
  prefix: null,
  suffix: null,
};

const callbacks = []; // 回調函數列表，當變數更新後觸發

// 設置共享變數的方法
export function setSharedElements(msg, { source, target, result, prefix, suffix }) {
  if (msg) sharedElements.errMsg = msg;
  if (source) sharedElements.source = source;
  if (target) sharedElements.target = target;
  if (result) sharedElements.result = result;
  if (prefix) sharedElements.prefix = prefix;
  if (suffix) sharedElements.suffix = suffix;

  // 通知所有回調
  callbacks.forEach((callback) => callback(sharedElements));
}

// 添加監聽器以處理變數初始化完成後的行為
export function onSharedElementsUpdated(callback) {
  if (typeof callback === 'function') {
    callbacks.push(callback);
  }
}
