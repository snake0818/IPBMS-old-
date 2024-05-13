const navItems = document.querySelectorAll('.nav-item');
const navMenu = document.querySelector('#nav-menu')

navItems.forEach(item => {
  item.addEventListener('mouseover', function () {
    // 將鼠標懸停的 nav-item 添加 active 類
    navMenu.classList.add('active');
  });
  item.addEventListener('mouseout', function () {
    // 將鼠標移出時移除 nav-item 上的 active 類
    setTimeout(() => {
      navMenu.classList.remove('active');
    }, 500)
  });
});