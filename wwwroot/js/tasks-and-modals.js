// --- 1. Obsługa dynamicznego ładowania modali (Unifikacja) ---
function openTaskModal(taskId, url, modalId, contentId) {
    const modalElement = document.getElementById(modalId);
    const contentElement = document.getElementById(contentId);

    if (!modalElement || !contentElement) return;

    const modal = new bootstrap.Modal(modalElement);
    modal.show();

    fetch(url + '/' + taskId)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            contentElement.innerHTML = html;

            // Inicjalizacja Tom Select, jeśli biblioteka jest dostępna i element istnieje
            if (typeof TomSelect !== 'undefined' && document.querySelector('.searchable-select')) {
                document.querySelectorAll('.searchable-select').forEach(el => {
                    new TomSelect(el, {
                        create: false,
                        sortField: { field: "text", direction: "asc" }
                    });
                });
            }
        })
        .catch(err => {
            contentElement.innerHTML = '<div class="alert alert-danger m-3">Błąd ładowania danych.</div>';
        });
}

// --- 2. Dynamiczne przełączanie trybów edycji (Zastępuje DOMContentLoaded) ---
document.addEventListener('click', function (e) {
    // Sprawdzamy czy kliknięto przycisk włączający edycję
    const toggleBtn = e.target.closest('#btn-toggle-edit');
    const cancelBtn = e.target.closest('#btn-cancel-edit');

    if (toggleBtn) {
        document.querySelectorAll('.view-mode').forEach(el => el.classList.add('d-none'));
        document.querySelectorAll('.edit-mode').forEach(el => el.classList.remove('d-none'));
    }

    if (cancelBtn) {
        document.querySelectorAll('.view-mode').forEach(el => el.classList.remove('d-none'));
        document.querySelectorAll('.edit-mode').forEach(el => el.classList.add('d-none'));
    }
});

// --- 3. Funkcje edycji komentarzy ---
function enableEdit(id) {
    const viewEl = document.getElementById('comment-view-' + id);
    const editEl = document.getElementById('comment-edit-' + id);
    if (viewEl) viewEl.style.display = 'none';
    if (editEl) editEl.style.display = 'block';
}

// Zmiana nazwy na unikalną, by nie gryzła się z natywnym cancelEdit w niektórych kontekstach
function discardCommentEdit(id) {
    const viewEl = document.getElementById('comment-view-' + id);
    const editEl = document.getElementById('comment-edit-' + id);
    if (viewEl) viewEl.style.display = 'block';
    if (editEl) editEl.style.display = 'none';
}

// --- 4. Pozostałe funkcje pomocnicze ---
function showLoading(element) {
    const form = element.closest('form');
    if (form) form.submit();
    element.style.opacity = "0.5";
    element.style.pointerEvents = "none";
}

function openAttachment(url) {
    const frame = document.getElementById("attachmentFrame");
    const modalEl = document.getElementById('attachmentModal');

    if (frame && modalEl) {
        frame.src = url;
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }
}