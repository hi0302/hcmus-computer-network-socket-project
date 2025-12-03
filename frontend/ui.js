document.addEventListener('DOMContentLoaded', () => {
    const buttons = document.querySelectorAll('.btn');
    const slideArea = document.getElementById('slideArea');

    buttons.forEach(btn => {
        btn.addEventListener('click', () => {
            const command = btn.dataset.command;
            const text = btn.querySelector('span').textContent;

        
            const msg = document.createElement('div');
            msg.classList.add('slide-message');
            msg.textContent = `Đã nhấn: ${text} | Gửi lệnh: ${command}`;

       
            slideArea.appendChild(msg);
            slideArea.scrollTop = slideArea.scrollHeight;

            if (command) sendCommand(command);
        });
    });
});