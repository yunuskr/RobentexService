(function(){
  const EDIT_DATA_URL = window.__RTX_EDIT_DATA_URL__;

  document.addEventListener('click', async (e) => {
    const btn = e.target.closest('.open-edit');
    if (!btn) return;

    const id = btn.dataset.id;

    try {
      const res = await fetch(`${EDIT_DATA_URL}/${id}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error(`GET ${res.status}`);
      const data = await res.json();

      // template → DOM
      const tpl = document.getElementById('edit-modal-tpl');
      if(!tpl) throw new Error('edit-modal-tpl bulunamadı');
      const frag = tpl.content.cloneNode(true);
      document.body.appendChild(frag);

      const root = document.querySelector('.rtx-modal-backdrop:last-of-type');
      const form = root.querySelector('#editForm');

      // Admin alanları
      setVal(form,'Id',                data.id);
      setVal(form,'Title',             data.title ?? '');
      setVal(form,'CustomerOrderNo',   data.customerOrderNo ?? '');
      setVal(form,'RobentexOrderNo',   data.robentexOrderNo ?? '');
      setVal(form,'TrackingNo',        data.trackingNo ?? '');
      form.querySelector('select[name="Status"]').value = (data.status ?? 0);

      // Readonly alanlar
      setDisp(root,'CompanyName',      data.companyName);
      setDisp(root,'FirstName',        data.firstName);
      setDisp(root,'LastName',         data.lastName);
      setDisp(root,'Phone',            data.phone);
      setDisp(root,'Email',            data.email);
      setDisp(root,'RobotModel',       data.robotModel);
      setDisp(root,'RobotSerial',      data.robotSerial);
      setDisp(root,'FaultDescription', data.faultDescription);
      setDisp(root,'CreatedAt',        fmtDate(data.createdAt));

      // --- Not listesi ARTIK GÖSTERİLMİYOR ---
      // Eğer partial içinde [data-notes] yoksa sorun çıkmaması için null-check:
      const notesWrap = root.querySelector('[data-notes]');
      if (notesWrap) {
        notesWrap.innerHTML = '';
        // Notları artık doldurmuyoruz. İleride tekrar göstermek istersen,
        // aşağıdaki bloğu açıp kullanabilirsin.
        /*
        if (Array.isArray(data.notes) && data.notes.length){
          data.notes.forEach(n=>{
            const div = document.createElement('div');
            div.className = 'note';
            div.innerHTML = `
              <div class="when">${fmtDate(n.createdAt)} — ${escapeHtml(n.createdBy || '-')}</div>
              <div class="txt">${escapeHtml(n.text || '')}</div>`;
            notesWrap.appendChild(div);
          });
        }
        */
      }

      // kapatma
      const close = () => { document.body.style.overflow=''; root.remove(); };
      root.addEventListener('click', (ev)=>{ if (ev.target === root || ev.target.hasAttribute('data-close')) close(); });
      root.querySelector('.x')?.addEventListener('click', close);
      window.addEventListener('keydown', (ev)=>{ if (ev.key === 'Escape') close(); }, { once:true });
      document.body.style.overflow = 'hidden';

      // kaydet
      form.addEventListener('submit', async (ev)=>{
        ev.preventDefault();
        const fd = new FormData(form);
        const resp = await fetch(form.action, {
          method:'POST', body: fd,
          headers:{ 'X-Requested-With':'XMLHttpRequest' },
          credentials:'same-origin'
        });
        if (resp.ok){ close(); location.reload(); }
        else { alert('Kaydedilemedi: ' + resp.status); }
      });

      // UX: ilk inputa odaklan
      form.querySelector('input[name="Title"]')?.focus();

    } catch (err) {
      console.error(err);
      alert('Düzenleme verileri yüklenemedi.');
    }
  });

  // yardımcılar
  function setVal(form, name, val){
    const el = form.querySelector(`[name="${name}"]`);
    if (el) el.value = val ?? '';
  }
  function setDisp(scope, key, val){
    const el = scope.querySelector(`[data-disp="${key}"]`);
    if(!el) return;
    const v = (val ?? '');
    if (el.tagName === 'TEXTAREA') el.value = v; else el.value = v;
  }
  function fmtDate(iso){
    if(!iso) return '';
    try{ const d = new Date(iso); return d.toLocaleString('tr-TR', {dateStyle:'short', timeStyle:'short'}); }catch{ return ''; }
  }
  function escapeHtml(s){
    return String(s ?? '').replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  }
})();
