// ── apiFetch ─────────────────────────────────────────────────────────────────
// Reads JWT from <meta name="auth-token"> injected by _Layout.cshtml
async function apiFetch(url, method = 'GET', body = null) {
    const token = document.querySelector('meta[name="auth-token"]')?.content || '';
    const opts = {
        method,
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': 'Bearer ' + token } : {})
        }
    };
    if (body !== null) opts.body = JSON.stringify(body);
    const res = await fetch(url, opts);
    if (!res.ok) {
        let msg = 'Request failed: ' + res.status;
        try { const d = await res.json(); msg = d.message || msg; } catch {}
        throw new Error(msg);
    }
    if (res.status === 204) return null;
    return res.json();
}

// ── showToast ─────────────────────────────────────────────────────────────────
function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:9999;display:flex;flex-direction:column;gap:8px;';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    const bg = type === 'error' ? '#EF4444' : type === 'warning' ? '#F59E0B' : '#10B981';
    toast.style.cssText = `background:${bg};color:#fff;padding:12px 20px;border-radius:8px;font-size:14px;font-weight:500;box-shadow:0 4px 12px rgba(0,0,0,0.15);opacity:0;transition:opacity 0.3s;max-width:320px;`;
    toast.textContent = message;
    container.appendChild(toast);
    requestAnimationFrame(() => { toast.style.opacity = '1'; });
    setTimeout(() => { toast.style.opacity = '0'; setTimeout(() => toast.remove(), 300); }, 3000);
}

// ── formatDate ────────────────────────────────────────────────────────────────
function formatDate(dateStr) {
    if (!dateStr) return '—';
    try { return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }); }
    catch { return '—'; }
}

// ── Sidebar ───────────────────────────────────────────────────────────────────
function openSidebar() {
    document.getElementById('sidebar')?.classList.add('open');
    const o = document.getElementById('sidebar-overlay');
    if (o) o.style.display = 'block';
}
function closeSidebar() {
    document.getElementById('sidebar')?.classList.remove('open');
    const o = document.getElementById('sidebar-overlay');
    if (o) o.style.display = 'none';
}

// ── Notifications ─────────────────────────────────────────────────────────────
function toggleNotifDropdown() {
    const dd = document.getElementById('notif-dropdown');
    if (!dd) return;
    const visible = dd.style.display !== 'none';
    dd.style.display = visible ? 'none' : 'block';
    if (!visible) loadNotifications();
}

async function loadNotifications() {
    const list = document.getElementById('notif-list');
    if (!list) return;
    try {
        const orders = await apiFetch('/api/orders');
        const pending = orders.filter(o => o.status === 'Pending' || o.status === 'In Progress');
        const badge = document.getElementById('notif-badge');
        const countBadge = document.getElementById('notif-count-badge');
        const footer = document.getElementById('notif-footer');
        if (pending.length > 0) {
            if (badge) { badge.style.display = 'flex'; badge.textContent = pending.length; }
            if (countBadge) { countBadge.style.display = 'inline-flex'; countBadge.textContent = pending.length; }
            if (footer) footer.style.display = 'block';
            list.innerHTML = pending.slice(0, 10).map(o => `
                <div style="padding:12px 16px;border-bottom:1px solid #F3F4F6;">
                    <p style="font-weight:600;font-size:14px;color:#111827;">${o.customerId?.customerName || 'Order #' + o._id}</p>
                    <p style="font-size:12px;color:#6B7280;">${(o.services || []).join(', ') || 'No services'}</p>
                    <span style="font-size:11px;font-weight:600;padding:2px 8px;border-radius:10px;background:${o.status === 'Pending' ? '#E5E7EB' : '#FDE68A'};color:${o.status === 'Pending' ? '#374151' : '#B45309'};">${o.status}</span>
                </div>`).join('');
        } else {
            if (badge) badge.style.display = 'none';
            list.innerHTML = '<div style="padding:32px;text-align:center;color:#9CA3AF;"><p>No pending orders</p></div>';
        }
    } catch {
        list.innerHTML = '<div style="padding:32px;text-align:center;color:#9CA3AF;">Failed to load</div>';
    }
}
