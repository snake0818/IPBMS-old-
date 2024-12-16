const scrollElements = document.querySelectorAll('.overflow-x-scroll');

scrollElements.forEach(element => {
  const scrollContent = element.querySelector('.card-container');

  // 動態監聽檢測滾動區域有無 overflow 發生
  const checkOverflow = (Container, Scorll) =>
    Container.classList.toggle('scroll', Scorll.scrollWidth > Container.clientWidth);
  new ResizeObserver(() => checkOverflow(element, scrollContent)).observe(scrollContent);

  // 滾輪事件偵測
  element.addEventListener('wheel', (event) => {
    // 判斷滾動方向
    const atEdge = (element.scrollLeft === 0 && event.deltaY < 0) ||
      (element.scrollLeft + element.clientWidth >= element.scrollWidth && event.deltaY > 0);

    // 向左/右滾到底，則向上/下滾動
    if (atEdge) { window.scrollBy({ top: event.deltaY, left: 0, behavior: 'smooth' }); }
    else { // 水平滾動
      event.preventDefault(); // 防止預設垂直滾動
      element.scrollLeft += event.deltaY;
    }
  }, { passive: true });
});