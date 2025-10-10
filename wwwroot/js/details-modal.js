(function () {
  const DETAILS_DATA_URL = window.__RTX_DETAILS_DATA_URL__;

  document.addEventListener("click", async (e) => {
    const btn = e.target.closest(".open-details");
    if (!btn) return;

    const id = btn.dataset.id;

    try {
      const res = await fetch(`${DETAILS_DATA_URL}/${id}`, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
        credentials: "same-origin",
      });
      if (!res.ok) throw new Error("GET " + res.status);
      const d = await res.json();

      // template'i ekle
      const tpl = document.getElementById("details-modal-tpl");
      if (!tpl) throw new Error("details-modal-tpl yok");
      document.body.appendChild(tpl.content.cloneNode(true));

      const root = document.querySelector(
        ".rtx-modal-backdrop:last-of-type"
      );
      const q = (sel) => root.querySelector(sel);

      // alanları doldur
      setVal(root, "CompanyName", d.companyName);
      setVal(root, "Title", d.title || "");
      setVal(root, "StatusText", d.statusText || "");
      setVal(root, "Requester", d.requesterName || "");
      setVal(root, "Phone", d.phone || "");
      setVal(root, "RobentexOrderNo", d.robentexOrderNo || "");
      setVal(root, "FaultDescription", d.faultDescription || "");
      setVal(root, "Email", d.email || "");
      setVal(root, "RobotModel", d.robotModel || "");
      setVal(root, "RobotSerial", d.robotSerial || "");
      setVal(root, "LastModified", fmtDate(d.lastModifiedUtc));

      // NOTLARI sil butonuyla render et
      const notesWrap = q("[data-notes]");
      renderNotes(notesWrap, d.notes);

      // kapatma
      const close = () => {
        document.body.style.overflow = "";
        root.remove();
      };
      root.addEventListener("click", (ev) => {
        if (ev.target === root || ev.target.hasAttribute("data-close")) close();
      });
      q(".x")?.addEventListener("click", close);
      window.addEventListener(
        "keydown",
        (ev) => {
          if (ev.key === "Escape") close();
        },
        { once: true }
      );
      document.body.style.overflow = "hidden";
    } catch (err) {
      console.error(err);
      alert("Detaylar yüklenemedi.");
    }
  });

  // ---- Notları render eden yardımcı
  function renderNotes(notesWrap, notes) {
    if (!notesWrap) return;
    notesWrap.innerHTML = "";

    if (!Array.isArray(notes) || notes.length === 0) {
      const empty = document.createElement("div");
      empty.className = "note";
      empty.innerHTML =
        '<div class="txt" style="opacity:.75">Henüz not yok.</div>';
      notesWrap.appendChild(empty);
      return;
    }

    notes.forEach((n) => {
      // ÖNEMLİ: backend JSON'unda her not için id olmalı (n.id)
      const id = n.id;
      const div = document.createElement("div");
      div.className = "note";
      div.setAttribute("data-note-id", id);

      div.innerHTML = `
        <div style="flex:1">
          <div class="when">${fmtDate(n.createdAt)} — ${esc(n.createdBy || "-")}</div>
          <div class="txt">${esc(n.text || "")}</div>
        </div>
        <button class="note-del" data-note-del="${id}">Sil</button>
      `;
      notesWrap.appendChild(div);
    });
  }

  // ---- Not silme (soft delete)
  document.addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-note-del]");
    if (!btn) return;

    const noteId = btn.getAttribute("data-note-del");
    if (!noteId) return;

    if (!confirm("Bu notu silmek istiyor musunuz?")) return;

    // modal içindeki gizli antiforgery token'ı al
    const token =
      document.querySelector(
        '#details-af-token input[name="__RequestVerificationToken"]'
      )?.value || "";

    try {
      const res = await fetch("/Admin/ServiceRequests/SoftDeleteNote", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
          RequestVerificationToken: token,
        },
        body: new URLSearchParams({ noteId }),
        credentials: "same-origin",
      });
      if (!res.ok) throw new Error(await res.text().catch(() => ""));

      btn.closest(".note")?.remove();
    } catch (err) {
      console.error(err);
      alert("Not silinirken bir hata oluştu.");
    }
  });

  // ---- yardımcılar
  function setVal(scope, key, val) {
    const el = scope.querySelector(`[data-dsp="${key}"]`);
    if (!el) return;
    el.value = val ?? "";
  }
  function fmtDate(iso) {
    if (!iso) return "";
    try {
      return new Date(iso).toLocaleString("tr-TR", {
        dateStyle: "short",
        timeStyle: "short",
      });
    } catch {
      return "";
    }
  }
  function esc(s) {
    return String(s ?? "").replace(/[&<>"']/g, (m) => ({
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#39;",
    })[m]);
  }
})();
