(function(){
  const DETAILS_DATA_URL = window.__RTX_DETAILS_DATA_URL__;

  document.addEventListener('click', async (e)=>{
    const btn = e.target.closest('.open-details');
    if(!btn) return;

    const id = btn.dataset.id;
    try{
      const res = await fetch(`${DETAILS_DATA_URL}/${id}`, {
        headers: {'X-Requested-With':'XMLHttpRequest'},
        credentials: 'same-origin'
      });
      if(!res.ok) throw new Error('GET ' + res.status);
      const d = await res.json();

      const tpl = document.getElementById('details-modal-tpl');
      if(!tpl) throw new Error('details-modal-tpl yok');
      document.body.appendChild(tpl.content.cloneNode(true));

      const root = document.querySelector('.rtx-modal-backdrop:last-of-type');
      const q = sel => root.querySelector(sel);

      setVal(root,'CompanyName', d.companyName);
      setVal(root,'Title',       d.title || '');
      setVal(root,'StatusText',  d.statusText || '');
      setVal(root,'Requester',   d.requesterName || '');
      setVal(root,'TrackingNo',  d.trackingNo || '');
      setVal(root,'Phone',       d.phone || '');
      setVal(root,'Email',       d.email || '');
      setVal(root,'RobotModel',  d.robotModel || '');
      setVal(root,'RobotSerial', d.robotSerial || '');
      setVal(root,'LastModified', fmtDate(d.lastModifiedUtc));

      const notesWrap = q('[data-notes]');
      notesWrap.innerHTML = '';
      if (Array.isArray(d.notes) && d.notes.length){
        d.notes.forEach(n=>{
          const div = document.createElement('div');
          div.className = 'note';
          div.innerHTML = `
            <div class="when">${fmtDate(n.createdAt)} — ${escapeHtml(n.createdBy || '-')}</div>
            <div class="txt">${escapeHtml(n.text || '')}</div>`;
          notesWrap.appendChild(div);
        });
      } else {
        const div = document.createElement('div');
        div.className = 'note';
        div.innerHTML = `<div class="txt" style="opacity:.75">Not bulunamadı.</div>`;
        notesWrap.appendChild(div);
      }

      const close = ()=>{ document.body.style.overflow=''; root.remove(); };
      root.addEventListener('click', ev=>{ if (ev.target === root || ev.target.hasAttribute('data-close')) close(); });
      q('.x')?.addEventListener('click', close);
      window.addEventListener('keydown', (ev)=>{ if (ev.key === 'Escape') close(); }, { once:true });
      document.body.style.overflow = 'hidden';

    }catch(err){
      console.error(err);
      alert('Detaylar yüklenemedi.');
    }
  });

  function setVal(scope, key, val){
    const el = scope.querySelector(`[data-dsp="${key}"]`);
    if(!el) return;
    el.value = val ?? '';
  }
  function fmtDate(iso){
    if(!iso) return '';
    try{ return new Date(iso).toLocaleString('tr-TR', {dateStyle:'short', timeStyle:'short'}); }catch{ return ''; }
  }
  function escapeHtml(s){
    return String(s ?? '').replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
  }
})();
