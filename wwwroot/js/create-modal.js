(function () {
  document.addEventListener('click', (e) => {
    const btn = e.target.closest('.open-create');
    if (!btn) return;

    // template → DOM
    const tpl = document.getElementById('create-modal-tpl');
    if (!tpl) { alert('Create modal şablonu bulunamadı'); return; }
    const frag = tpl.content.cloneNode(true);
    document.body.appendChild(frag);

    const root = document.querySelector('.rtx-modal-backdrop:last-of-type');
    const form = root.querySelector('#createForm');

    const close = () => { document.body.style.overflow=''; root.remove(); };
    root.addEventListener('click', ev => { if (ev.target === root || ev.target.hasAttribute('data-close')) close(); });
    root.querySelector('.x')?.addEventListener('click', close);
    window.addEventListener('keydown', ev => { if (ev.key === 'Escape') close(); }, { once:true });
    document.body.style.overflow='hidden';

    // İlk inputa odak
    form.querySelector('input[name="CompanyName"]')?.focus();

    // Kaydet
    form.addEventListener('submit', async (ev) => {
      ev.preventDefault();
      try {
        const fd = new FormData(form);
        const resp = await fetch(form.action, {
          method: 'POST',
          body: fd,
          headers: { 'X-Requested-With': 'XMLHttpRequest' },
          credentials: 'same-origin'
        });
        if (resp.ok) { close(); location.reload(); }
        else {
          const text = await resp.text().catch(()=>resp.statusText);
          alert('Oluşturulamadı: ' + resp.status + ' ' + text);
        }
      } catch (err) {
        console.error(err);
        alert('Beklenmeyen hata.');
      }
    });
  });
})();
