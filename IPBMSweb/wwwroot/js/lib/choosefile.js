const file = document.getElementById('uploadFile');
const img = document.getElementById('ShowImage');
const video = document.getElementById('ShowVideo')

const block1 = document.getElementById('block1');
const block2 = document.getElementById('block2');

file.onchange = (e) => {
    // 圖片處理
    if (file.files[0]) {
        //  新建臨時圖片url
        img.src = URL.createObjectURL(file.files[0]);
        block1.style.display = 'none';
        block2.style.display = 'block';
    } else { 
        img.src = '';
        block1.style.display = 'block';
        block2.style.display = 'none';
    }
}