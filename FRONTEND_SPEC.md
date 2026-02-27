# Frontend Integration Spec — Portfolio Backend API

What the frontend needs to integrate with the backend. Share this with the frontend developer.

---

## 1. Base URL & Environment

- **Production:** `https://api.kanellos.me`
- **Local dev:** `http://localhost:5000` (or whatever port the backend runs on)

Set an env variable, e.g. `VITE_BACKEND_URL` or `NEXT_PUBLIC_API_URL`, and use it for all API calls.

---

## 2. Authentication

The API uses **JWT in an HttpOnly cookie** named `access_token`.

- **Login / Register:** The backend sets the cookie automatically.
- **All authenticated requests:** Send `credentials: 'include'` so the browser includes the cookie.
- **Logout:** Call the logout endpoint; the backend clears the cookie.

**Important:** All `fetch` calls to the API must use:

```javascript
fetch(`${API_URL}/api/auth/login`, {
  method: 'POST',
  credentials: 'include',  // Required for cookies
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
})
```

Without `credentials: 'include'`, cookies are not sent and auth will fail (especially cross-origin).

---

## 3. CORS

The backend allows:

- `https://kanellos.me`
- `https://www.kanellos.me`
- `http://localhost:5173` (Vite dev)
- `https://localhost:5001`

If you add a new origin (e.g. staging), it must be added to the backend config.

---

## 4. API Endpoints Summary

### Auth (`/api/auth`)

| Method | Path | Auth | Body | Purpose |
|--------|------|------|------|---------|
| POST | `/api/auth/register` | No | `{ email, password, username? }` | Create account |
| POST | `/api/auth/login` | No | `{ email, password }` | Log in |
| GET | `/api/auth/me` | Yes | — | Get current user |
| PUT | `/api/auth/me` | Yes | `{ username }` | Update username |
| POST | `/api/auth/logout` | No | — | Log out |
| POST | `/api/auth/forgot-password` | No | `{ email }` | Request password reset |
| POST | `/api/auth/reset-password` | No | `{ token, newPassword }` | Reset password with token |
| GET | `/api/auth/users` | Admin | — | List all users |
| GET | `/api/auth/users/{id}` | Admin | — | Get user by id |
| PUT | `/api/auth/users/{id}` | Admin | `{ username?, email?, isAdmin? }` | Edit user |

**Login response:** `{ isAdmin: boolean, username: string }`  
**Me response:** `{ userId, email, username, isAdmin }`

---

### Tickets (`/ticket/TicketSubmit`)

| Method | Path | Auth | Body | Purpose |
|--------|------|------|------|---------|
| POST | `/ticket/TicketSubmit/submit` | Yes | `{ title, description, category }` | Create ticket |
| POST | `/ticket/TicketSubmit/{ticketId}/reply` | Owner/Admin | `{ message }` | Reply to ticket |
| GET | `/ticket/TicketSubmit/{ticketId}/replies` | Owner/Admin | — | Get replies |
| GET | `/ticket/TicketSubmit/get` | Yes | — | List tickets (admin: all, user: own) |
| GET | `/ticket/TicketSubmit/get/{id}` | Owner/Admin | — | Get one ticket |
| PUT | `/ticket/TicketSubmit/update/{id}` | Admin | `{ title?, description?, category? }` | Update ticket |
| DELETE | `/ticket/TicketSubmit/delete/{id}` | Admin | — | Delete ticket |
| PUT | `/ticket/TicketSubmit/resolve/{id}` | Admin | — | Mark resolved |
| PUT | `/ticket/TicketSubmit/reopen/{id}` | Admin | — | Reopen ticket |

**Ticket shape:** `{ id, title, description, category, createdAt, updatedAt, isResolved, userEmail }`  
**Reply shape:** `{ id, ticketId, userId, userEmail, message, createdAt }`

---

### Showcase (`/api/showcase`)

| Method | Path | Auth | Body | Purpose |
|--------|------|------|------|---------|
| GET | `/api/showcase` | Yes | — | List items (admin: all, user: assigned) |
| POST | `/api/showcase` | Admin | See below | Create item |
| GET | `/api/showcase/slug/{slug}` | Yes | — | Get item by slug |
| POST | `/api/showcase/upload` | Yes | FormData, field `file` | Upload image |
| DELETE | `/api/showcase/{id}` | Admin | — | Delete item |

**Create item body:**

- Site: `{ type: "site", title, slug?, userIds, htmlContent }`
- Photo: `{ type: "photo", title, slug?, userIds, imageUrl }`

**Item shape:** `{ id, type, title, slug, htmlContent?, imageUrl?, userIds, createdAt, updatedAt }`  
**Upload response:** `{ url: "https://..." }` (max 5MB, images only)

---

### Cloudflare Analytics (`/api/cloudflare`)

| Method | Path | Auth | Query | Purpose |
|--------|------|------|-------|---------|
| GET | `/api/cloudflare/analytics/dashboard` | Yes | `since`, `until` (ISO 8601) | Get analytics data |

**Response:** Raw Cloudflare GraphQL JSON (frontend sums `count`, `visits`, etc.).

---

## 5. What the Frontend Needs to Build

### Public (no auth)

- [ ] **Login page** — form: email, password → POST `/api/auth/login`
- [ ] **Register page** — form: email, password, username (optional) → POST `/api/auth/register`
- [ ] **Forgot password** — form: email → POST `/api/auth/forgot-password`
- [ ] **Reset password page** — URL has `?token=xxx`; form: newPassword → POST `/api/auth/reset-password` with `{ token, newPassword }`

### After login

- [ ] **Current user** — on app load or after login: GET `/api/auth/me` → show username, admin status
- [ ] **Update username** — form → PUT `/api/auth/me` with `{ username }`
- [ ] **Logout** — POST `/api/auth/logout`

### Tickets (support)

- [ ] **Submit ticket** — form: title, description, category → POST `/ticket/TicketSubmit/submit`
- [ ] **My tickets** — GET `/ticket/TicketSubmit/get` → list view
- [ ] **Ticket detail** — GET `/ticket/TicketSubmit/get/{id}` → show ticket + replies
- [ ] **Add reply** — form: message → POST `/ticket/TicketSubmit/{ticketId}/reply`
- [ ] **Replies list** — GET `/ticket/TicketSubmit/{ticketId}/replies`

### Admin only

- [ ] **User list** — GET `/api/auth/users`
- [ ] **User detail / edit** — GET `/api/auth/users/{id}`, PUT `/api/auth/users/{id}`
- [ ] **Ticket admin** — update, delete, resolve, reopen tickets
- [ ] **Showcase admin** — create, delete showcase items; upload images
- [ ] **Analytics** — GET `/api/cloudflare/analytics/dashboard` (e.g. for `analytics.kanellos.me`)

### Showcase (client-facing)

- [ ] **Showcase list** — GET `/api/showcase` → show items assigned to current user
- [ ] **Showcase detail** — GET `/api/showcase/slug/{slug}` → render site (htmlContent) or photo (imageUrl)

---

## 6. Error Handling

- **401** — Not logged in; redirect to login or show login modal
- **403** — Forbidden (e.g. not admin); show “access denied”
- **404** — Resource not found
- **400** — Validation error; response may be `{ message: "..." }` or `{ errors: { field: ["..."] } }`

Always read the response body for error messages to display to the user.

---

## 7. Full API Reference

The backend repo contains `ENDPOINTS.md` with full request/response examples. Use it as the source of truth when implementing each endpoint.

---

## 8. Example: Authenticated Request

```javascript
const API_URL = import.meta.env.VITE_BACKEND_URL ?? 'https://api.kanellos.me';

async function getMe() {
  const res = await fetch(`${API_URL}/api/auth/me`, {
    credentials: 'include'
  });
  if (res.status === 401) return null;
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

---

## 9. Password Reset Flow (for frontend)

1. User goes to “Forgot password” and enters email.
2. Frontend: POST `/api/auth/forgot-password` with `{ email }`.
3. Backend emails a link like `{FRONTEND_URL}/reset-password?token=abc123`.
4. User clicks link and lands on `/reset-password?token=abc123`.
5. Frontend: form with “New password” and “Confirm” → POST `/api/auth/reset-password` with `{ token, newPassword }`.
6. On success: redirect to login.

The backend uses `FRONTEND_URL` from env to build the reset link. Ensure it matches your frontend (e.g. `https://kanellos.me`).
