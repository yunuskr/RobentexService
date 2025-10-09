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

      // Notlar (gösterilmiyorsa dokunma)
      const notesWrap = root.querySelector('[data-notes]');
      if (notesWrap) notesWrap.innerHTML = '';

      // Kapatma
      const close = () => { document.body.style.overflow=''; root.remove(); };
      root.addEventListener('click', (ev)=>{ if (ev.target === root || ev.target.hasAttribute('data-close')) close(); });
      root.querySelector('.x')?.addEventListener('click', close);
      window.addEventListener('keydown', (ev)=>{ if (ev.key === 'Escape') close(); }, { once:true });
      document.body.style.overflow = 'hidden';

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
})();

// === TEK SUBMIT HANDLER (Kaydet + Sil/SoftDelete) ===
document.addEventListener('submit', async (e) => {
  const form = e.target;
  if (!form || form.id !== 'editForm') return;

  e.preventDefault();
  if (typeof e.stopImmediatePropagation === 'function') e.stopImmediatePropagation();

  // Çifte submit kilidi
  if (form.dataset.submitting === '1') return;
  form.dataset.submitting = '1';

  try {
    const submitter = e.submitter || document.activeElement || form.querySelector('[type="submit"]');
    const url = (submitter && submitter.getAttribute('formaction')) || form.action;
    const method = (submitter && submitter.getAttribute('formmethod')) || form.method || 'post';

    // Sadece JS confirm (butonda onclick olmasın)
    if (submitter && submitter.classList.contains('danger')) {
      const ok = confirm('Bu talebi silindi olarak işaretlemek istediğinize emin misiniz?');
      if (!ok) { form.dataset.submitting = '0'; return; }
    }

    const fd = new FormData(form);
    const res = await fetch(url, { method, body: fd, credentials: 'same-origin' });

    if (res.ok) {
      try { await res.text(); } catch {}
      const modal = form.closest('.rtx-modal-backdrop');
      if (modal) modal.remove();
      setTimeout(() => window.location.reload(), 350);
      return;
    }

    const msg = await res.text().catch(() => '');
    alert(msg || 'İşlem başarısız.');
    form.dataset.submitting = '0';
  } catch (err) {
    console.error(err);
    alert('İşlem sırasında bir hata oluştu.');
    form.dataset.submitting = '0';
  }
});
