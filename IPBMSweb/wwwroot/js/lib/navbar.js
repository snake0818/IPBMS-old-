const nav = document.querySelector('#nav');
const navItems = document.querySelectorAll('.nav-item');
const navSubmenus = document.querySelectorAll('.nav-submenu');

nav.addEventListener('mouseleave', function(){
  navItems.forEach(item => { item.classList.remove('active'); });
  this.classList.remove('nav-submenu-open');
  this.classList.remove('nav-curtain-open');
  this.classList.add('nav-animating');
  this.classList.add('nav-submenu-closing');
  setTimeout(() => {
    this.classList.remove('nav-submenu-open');
    this.classList.remove('nav-animating');
    this.classList.remove('nav-submenu-closing');
  }, 500);
});
navItems.forEach(item => {
  item.addEventListener('mouseover', function () {
    if (!this.classList.contains('active')) {
      navItems.forEach(item => { item.classList.remove('active'); });
      this.classList.add('active')
      nav.classList.add('nav-animating');
      nav.classList.add('nav-curtain-open');
      setTimeout(() => {
        nav.classList.add('nav-submenu-open');
        nav.classList.remove('nav-animating');
      }, 500);
    }
  });
});