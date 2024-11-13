const SELECT = ".custom-select";
const SELECTED = ".select-box";
const OPTIONS = ".select-options";
const OPTION = ".option-item";
const ShowStatus = "show";

function handleSelectClick(event) {
  const target = event.target; // 獲取點擊的元素

  if (target.closest(SELECTED)) { // 點擊的是 select-box 時，打開/關閉下拉選單
    const optionsList = target.closest(SELECT).querySelector(OPTIONS);
    toggleDropdown(optionsList);
  }

  // 點擊為 option 時，選取並更新文字
  if (target.closest(OPTION)) { selectOption(target.closest(OPTION)); }
}

function toggleDropdown(optionsList) {
  // 以防其他選單同時開啟
  document.querySelectorAll(OPTIONS).forEach(item => item !== optionsList && item.classList.remove(ShowStatus));
  optionsList.classList.toggle(ShowStatus); // 切換當前選單的顯示狀態
}

function selectOption(optionElement) {
  // 找到最近的 .custom-select 父層
  const customSelect = optionElement.closest(SELECT);
  const selectBox = customSelect.querySelector(SELECTED);
  selectBox.textContent = optionElement.textContent; // 更新 .select-box 中的文字
  selectBox.dataset.selected = optionElement.dataset.value; // 更新 .select-box 中的變數
  toggleDropdown(customSelect.querySelector(OPTIONS)); // 關閉當前下拉選單
}
