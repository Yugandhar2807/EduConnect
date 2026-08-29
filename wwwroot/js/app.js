/* ==========================================================================
   EduConnect application shell: sidebar, toasts, confirm dialogs,
   data-table search/sort/pagination, antiforgery helper.
   ========================================================================== */

window.App = (function () {
    'use strict';

    /* ---------- Antiforgery ---------- */
    function token() {
        const input = document.querySelector('#__af input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    /* ---------- Toasts ---------- */
    function toast(message, type) {
        type = type || 'info';
        let container = document.querySelector('.app-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'app-toast-container';
            document.body.appendChild(container);
        }
        const icons = { success: 'fa-circle-check', error: 'fa-circle-exclamation', info: 'fa-circle-info' };
        const el = document.createElement('div');
        el.className = 'app-toast ' + type;
        el.setAttribute('role', 'alert');
        el.innerHTML =
            '<i class="fa-solid ' + (icons[type] || icons.info) + ' toast-icon"></i>' +
            '<div class="toast-message"></div>' +
            '<button type="button" class="btn-close" aria-label="Close"></button>';
        el.querySelector('.toast-message').textContent = message;
        el.querySelector('.btn-close').addEventListener('click', function () { el.remove(); });
        container.appendChild(el);
        setTimeout(function () {
            el.style.transition = 'opacity .3s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 320);
        }, 5000);
    }

    /* ---------- Confirm dialog ---------- */
    let confirmModal = null;
    let confirmCallback = null;

    function ensureConfirmModal() {
        if (confirmModal) return confirmModal;
        const wrapper = document.createElement('div');
        wrapper.innerHTML =
            '<div class="modal fade" id="appConfirmModal" tabindex="-1" aria-hidden="true">' +
            '  <div class="modal-dialog modal-dialog-centered modal-sm" style="max-width:400px">' +
            '    <div class="modal-content">' +
            '      <div class="modal-body text-center p-4">' +
            '        <div class="empty-icon mx-auto mb-3" style="width:56px;height:56px;background:var(--danger-soft);color:var(--danger);border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:1.3rem"><i class="fa-solid fa-triangle-exclamation"></i></div>' +
            '        <h5 class="mb-2">Are you sure?</h5>' +
            '        <p class="text-muted mb-4" id="appConfirmMessage"></p>' +
            '        <div class="d-flex gap-2 justify-content-center">' +
            '          <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>' +
            '          <button type="button" class="btn btn-danger" id="appConfirmOk">Confirm</button>' +
            '        </div>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(wrapper.firstElementChild);
        const modalEl = document.getElementById('appConfirmModal');
        modalEl.querySelector('#appConfirmOk').addEventListener('click', function () {
            bootstrap.Modal.getInstance(modalEl).hide();
            if (confirmCallback) { const cb = confirmCallback; confirmCallback = null; cb(); }
        });
        confirmModal = modalEl;
        return modalEl;
    }

    function confirm(message, callback) {
        const modalEl = ensureConfirmModal();
        modalEl.querySelector('#appConfirmMessage').textContent = message || 'This action cannot be undone.';
        confirmCallback = callback;
        new bootstrap.Modal(modalEl).show();
    }

    function bindConfirms(root) {
        (root || document).querySelectorAll('form[data-confirm]').forEach(function (form) {
            if (form.dataset.confirmBound) return;
            form.dataset.confirmBound = '1';
            form.addEventListener('submit', function (e) {
                if (form.dataset.confirmed === '1') { form.dataset.confirmed = ''; return; }
                e.preventDefault();
                confirm(form.dataset.confirm, function () {
                    form.dataset.confirmed = '1';
                    form.requestSubmit();
                });
            });
        });
    }

    /* ---------- Data tables: search / sort / pagination ---------- */
    function initDataTable(card) {
        const table = card.querySelector('table');
        if (!table || !table.tBodies.length) return;

        const tbody = table.tBodies[0];
        const allRows = Array.from(tbody.rows);
        const pageSize = parseInt(table.dataset.pageSize || '10', 10);
        const searchInput = card.querySelector('[data-table-search]');
        const paginationEl = card.querySelector('[data-table-pagination]');
        let filtered = allRows.slice();
        let currentPage = 1;
        let sortCol = -1;
        let sortDir = 1;

        function cellValue(row, index, type) {
            const cell = row.cells[index];
            if (!cell) return '';
            const raw = (cell.dataset.value !== undefined ? cell.dataset.value : cell.textContent).trim();
            if (type === 'num') { const n = parseFloat(raw.replace(/[^\d.-]/g, '')); return isNaN(n) ? -Infinity : n; }
            if (type === 'date') { const d = Date.parse(raw); return isNaN(d) ? 0 : d; }
            return raw.toLowerCase();
        }

        function render() {
            const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
            if (currentPage > totalPages) currentPage = totalPages;
            const start = (currentPage - 1) * pageSize;
            const visible = filtered.slice(start, start + pageSize);

            allRows.forEach(function (row) { row.style.display = 'none'; });
            visible.forEach(function (row) { row.style.display = ''; });

            let emptyRow = tbody.querySelector('.dt-empty-row');
            if (!filtered.length) {
                if (!emptyRow) {
                    emptyRow = document.createElement('tr');
                    emptyRow.className = 'dt-empty-row';
                    const colCount = table.tHead ? table.tHead.rows[0].cells.length : 1;
                    emptyRow.innerHTML = '<td colspan="' + colCount + '" class="text-center text-muted py-4"><i class="fa-solid fa-magnifying-glass me-2"></i>No matching records found</td>';
                    tbody.appendChild(emptyRow);
                }
                emptyRow.style.display = '';
            } else if (emptyRow) {
                emptyRow.style.display = 'none';
            }

            if (paginationEl) {
                if (filtered.length <= pageSize) {
                    paginationEl.innerHTML = filtered.length
                        ? '<span>Showing ' + filtered.length + ' of ' + allRows.length + ' records</span>'
                        : '';
                    return;
                }
                let html = '<span>Showing ' + (start + 1) + '–' + Math.min(start + pageSize, filtered.length) + ' of ' + filtered.length + '</span>';
                html += '<div class="btn-group btn-group-sm" role="group">';
                html += '<button type="button" class="btn btn-outline-secondary" data-page="prev" ' + (currentPage === 1 ? 'disabled' : '') + '><i class="fa-solid fa-chevron-left"></i></button>';

                const pages = [];
                for (let p = 1; p <= totalPages; p++) {
                    if (p === 1 || p === totalPages || Math.abs(p - currentPage) <= 1) pages.push(p);
                    else if (pages[pages.length - 1] !== '...') pages.push('...');
                }
                pages.forEach(function (p) {
                    if (p === '...') html += '<button type="button" class="btn btn-outline-secondary" disabled>…</button>';
                    else html += '<button type="button" class="btn ' + (p === currentPage ? 'btn-primary' : 'btn-outline-secondary') + '" data-page="' + p + '">' + p + '</button>';
                });
                html += '<button type="button" class="btn btn-outline-secondary" data-page="next" ' + (currentPage === totalPages ? 'disabled' : '') + '><i class="fa-solid fa-chevron-right"></i></button>';
                html += '</div>';
                paginationEl.innerHTML = html;
                paginationEl.querySelectorAll('[data-page]').forEach(function (btn) {
                    btn.addEventListener('click', function () {
                        const val = btn.dataset.page;
                        if (val === 'prev') currentPage = Math.max(1, currentPage - 1);
                        else if (val === 'next') currentPage = Math.min(totalPages, currentPage + 1);
                        else currentPage = parseInt(val, 10);
                        render();
                    });
                });
            }
        }

        if (searchInput) {
            searchInput.addEventListener('input', function () {
                const q = searchInput.value.trim().toLowerCase();
                filtered = q
                    ? allRows.filter(function (row) { return row.textContent.toLowerCase().indexOf(q) !== -1; })
                    : allRows.slice();
                currentPage = 1;
                render();
            });
        }

        if (table.tHead) {
            Array.from(table.tHead.rows[0].cells).forEach(function (th, index) {
                if (!th.hasAttribute('data-sort')) return;
                th.addEventListener('click', function () {
                    const type = th.getAttribute('data-sort') || 'text';
                    if (sortCol === index) sortDir = -sortDir; else { sortCol = index; sortDir = 1; }
                    Array.from(table.tHead.rows[0].cells).forEach(function (cell) { cell.classList.remove('sorted-asc', 'sorted-desc'); });
                    th.classList.add(sortDir === 1 ? 'sorted-asc' : 'sorted-desc');
                    filtered.sort(function (a, b) {
                        const va = cellValue(a, index, type);
                        const vb = cellValue(b, index, type);
                        if (va < vb) return -1 * sortDir;
                        if (va > vb) return 1 * sortDir;
                        return 0;
                    });
                    render();
                });
            });
        }

        render();
    }

    /* ---------- Sidebar ---------- */
    function initSidebar() {
        const toggle = document.querySelector('.menu-toggle');
        const backdrop = document.querySelector('.sidebar-backdrop');
        if (toggle) toggle.addEventListener('click', function () { document.body.classList.toggle('sidebar-open'); });
        if (backdrop) backdrop.addEventListener('click', function () { document.body.classList.remove('sidebar-open'); });
    }

    /* ---------- Fetch helper (JSON POST with antiforgery) ---------- */
    function postJson(url, body) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token()
            },
            body: body === undefined ? undefined : JSON.stringify(body)
        }).then(function (r) {
            if (!r.ok) throw new Error('Request failed (' + r.status + ')');
            return r.json();
        });
    }

    /* ---------- Init ---------- */
    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        bindConfirms(document);
        document.querySelectorAll('[data-table]').forEach(initDataTable);
    });

    return { toast: toast, confirm: confirm, token: token, postJson: postJson };
})();
