# Tailor Management System — Project Context

## Stack
- ASP.NET Core 9 MVC + Razor Views
- SQLite (TailorDb.db) via Entity Framework Core
- JWT Bearer auth for API endpoints (`[Authorize]`)
- Session-based auth for Razor MVC views
- React SPA (Vite build) served from `wwwroot/` as fallback
- Runs on `http://localhost:5000`

---

## Architecture

### Two frontends in one app
1. **Razor MVC views** — `/Auth/Login`, `/Auth/Register`, `/Home/*` pages (Customers, Orders, Tailors, Inventory, Income, Appointment, Profile, Dashboard)
2. **React SPA** — served from `wwwroot/index.html` as fallback for all other routes

### Auth flow
- Login → generates JWT → stored in `HttpContext.Session["token"]`
- `_Layout.cshtml` injects token into `<meta name="auth-token">` tag
- `site.js` reads that meta tag and sends `Authorization: Bearer <token>` on every `apiFetch()` call
- API controllers use `[Authorize]` with JWT Bearer scheme

---

## File Structure (key files)

```
Tailor-Management-System/
├── Controllers/
│   ├── AuthController.cs          — Login, Register, Logout, /api/auth/google
│   ├── HomeController.cs          — Razor view routing
│   └── Api/
│       ├── CustomersApiController.cs
│       ├── TailorsApiController.cs
│       ├── InventoryApiController.cs
│       ├── OrdersApiController.cs
│       ├── MeasurementsApiController.cs
│       └── IncomeApiController.cs
├── Models/
│   ├── Customer.cs                — Id, UserId, CustomerName, ContactNo, Address
│   ├── Tailor.cs                  — Id, UserId, Name, Phone, Address, PaymentType, Salary, ContractRate
│   ├── InventoryItem.cs           — Id, UserId, ItemName, Category, StockQty, StockUnit, UnitPrice
│   ├── Order.cs                   — Id, UserId, CustomerId, AssignedTailorId, MeasurementId, Services(JSON), Status, PaymentMethod, UserDefinedTotal, TargetDeliveryDate
│   ├── IncomeItem.cs              — Id, UserId, CustomerName, OrderId, Amount, PaymentMethod, Date
│   ├── Measurement.cs             — Id, UserId, CustomerId, Title, shirt/pant measurements (JSON)
│   └── User.cs                    — Id, FullName, ShopName, Email, Password(BCrypt)
├── Views/
│   ├── Auth/Login.cshtml          — Google Sign-In + email/password login
│   ├── Auth/Register.cshtml
│   ├── Shared/_Layout.cshtml      — Sidebar, header, injects auth-token meta tag, loads site.js + site.css
│   └── Home/
│       ├── Customers.cshtml
│       ├── Tailors.cshtml
│       ├── Orders.cshtml
│       ├── Inventory.cshtml
│       ├── Income.cshtml
│       ├── Appointment.cshtml
│       ├── Profile.cshtml
│       └── Index.cshtml           — Dashboard
├── wwwroot/
│   ├── js/site.js                 ← CRITICAL — must exist, defines apiFetch/showToast/formatDate
│   ├── css/site.css               ← CRITICAL — must exist (can be empty or have styles)
│   ├── assets/index-DhqI8ZcJ.js  — React SPA bundle
│   └── index.html                 — React SPA entry
├── Data/TailorDbContext.cs
├── Program.cs
└── appsettings.json               — JWT config, SQLite connection string
```

---

## Critical Dependency: site.js

`wwwroot/js/site.js` MUST exist and define these globals used by ALL Razor views:

```js
apiFetch(url, method, body)   // reads JWT from <meta name="auth-token">, sends Bearer token
showToast(message, type)      // success/error toast notifications
formatDate(dateStr)           // formats dates for display
openSidebar() / closeSidebar()
toggleNotifDropdown()
loadNotifications()
```

**If site.js is deleted (e.g. when user replaces code), ALL CRUD breaks silently.**

### site.js template (recreate if missing):

```js
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

function formatDate(dateStr) {
    if (!dateStr) return '—';
    try { return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }); }
    catch { return '—'; }
}

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
```

---

## Known Issues & Status

### ✅ FIXED
- `site.js` missing → recreated at `wwwroot/js/site.js`
- `site.css` missing → recreated at `wwwroot/css/site.css`
- Google OAuth `origin_mismatch` → updated client ID to `427632404779-s7v6ad03k62vjvug09o0mrtn7la358b0.apps.googleusercontent.com` in Login.cshtml, Register.cshtml, and wwwroot/assets/index-DhqI8ZcJ.js
- Google login backend → added `/api/auth/google` endpoint in AuthController that verifies Google ID token via `https://oauth2.googleapis.com/tokeninfo`
- API controllers returning plain C# objects → `id` field only, but views expected `_id` (MongoDB-style)
  - Fixed: CustomersApiController, TailorsApiController, InventoryApiController now return both `_id` and `id` in all GET/POST/PUT responses

### ⚠️ PENDING / NEEDS TESTING AFTER RESTART
- **Customer delete** — was broken because `c._id` was `undefined`. Fix applied, needs restart to verify.
- **Tailor edit/delete** — same root cause. Fix applied.
- **Inventory CRUD** — same root cause. Fix applied.
- **Orders CRUD** — OrdersApiController already returned `_id` via `BuildOrderResponse()`. Should work but needs testing.
- **Phone number validation** — added `length !== 10` check in Tailors.cshtml and Customers.cshtml.

### ❌ NOT FIXED YET
- **Google Sign-In for institutional accounts** (`@rku.ac.in`) — Google Workspace blocks OAuth for unverified apps. User must either use a personal Gmail OR add the account as a Test User in Google Cloud Console → OAuth consent screen.
- **App crashes on restart if another process holds the .exe** — stop the running process in Visual Studio or Task Manager first.

---

## How to Restart the App

```bash
dotnet run
```
Run from: `Tailor-Management-System/` directory. URL: `http://localhost:5000`

**If build fails with "file locked":** Stop the app in Visual Studio or kill `Tailor-Management-System.exe` in Task Manager first.

---

## Google OAuth Config

- Client ID: `427632404779-s7v6ad03k62vjvug09o0mrtn7la358b0.apps.googleusercontent.com`
- Client Secret: `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`
- Authorized JS origins: `http://localhost:5000`
- Authorized redirect URIs: `http://localhost:5000/signin-google`
- Backend endpoint: `POST /api/auth/google` with body `{ googleToken: "<id_token>" }`

---

## API Endpoints Summary

| Method | URL | Description |
|--------|-----|-------------|
| POST | /api/auth/login | Login (React) |
| POST | /api/auth/register | Register (React) |
| POST | /api/auth/google | Google OAuth login |
| GET/POST | /api/customers | List / Create customer |
| PUT/DELETE | /api/customers/{id} | Update / Delete customer |
| GET/POST | /api/tailors | List / Create tailor |
| PUT/DELETE | /api/tailors/{id} | Update / Delete tailor |
| GET/POST | /api/inventory | List / Create item |
| PUT/DELETE | /api/inventory/{id} | Update / Delete item |
| GET/POST | /api/orders | List / Create order |
| PUT/DELETE | /api/orders/{id} | Update / Delete order |
| GET/POST | /api/measurements | List / Create measurement |
| PUT/DELETE | /api/measurements/{id} | Update / Delete measurement |
| GET | /api/income | List income entries |

---

## Common Pitfalls

1. **`site.js` gets deleted** when user replaces code — must recreate it every time (template above)
2. **`_id` vs `id`** — Razor views use `_id`. All API controllers must return both `_id` and `id`
3. **JWT not sent** — if session expired, all API calls return 401. User must re-login.
4. **Build locked** — Visual Studio holds the .exe. Stop VS debugger before `dotnet run`.
5. **`services` field in Orders** — stored as JSON string in SQLite, deserialized back to array in `BuildOrderResponse()`
