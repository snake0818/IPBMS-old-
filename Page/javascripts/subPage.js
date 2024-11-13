export function openSubPage(constentHTML) { // 創建子頁面的HTML內容
  // 檢查是否已有子頁面存在，避免重複創建
  if (document.getElementById('subPage')) return;

  // 創建子頁面的HTML內容
  var subPageHTML = `
  <div class="subPage" id="subPage">
      <div class="subContent">
          <!-- 子頁面的內容 -->
          ${constentHTML}
      </div>
  </div>
  `;

  // 將子頁面的HTML內容插入到body的末尾
  document.body.insertAdjacentHTML('beforeend', subPageHTML);

  var subPage = document.getElementById('subPage'); // 取得 subPage 元素

  // 增加進場動畫執行
  // 強制瀏覽器重繪，確保動畫能正確觸發
  void subPage.offsetWidth;
  subPage.classList.add('active');

  // 當點擊模糊背景時，關閉子頁面
  subPage.addEventListener('click', closeSubPage);
}

function closeSubPage(event) {
  const element = document.getElementById('subPage'); // 取得 subPage 元素
  if (event.target === element && element) {
    element.classList.remove('active');
    // 等待動畫結束後移除元素
    element.addEventListener('transitionend', () => element.remove(), { once: true });
  }
}