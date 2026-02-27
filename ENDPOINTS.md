# Portfolio Backend – All Endpoints

Base URL: `http://localhost:5215` (or your host).  
Auth: JWT in cookie `access_token` or Bearer header. `[Auth]` = requires login.

---

## Auth (`/api/auth`)

### 1. Register (create account)

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/auth/register` |
| **Auth** | No |

**Request body (JSON):**
```json
{
  "email": "user@example.com",
  "password": "YourSecurePassword",
  "username": "optional_username"
}
```

**Success:** `200 OK` (empty body). Cookie `access_token` set with JWT.  
**Errors:** `400` "User already exists.", "Username is already taken.", `409` duplicate, `500` database error.

---

### 2. Login

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/auth/login` |
| **Auth** | No |

**Request body (JSON):**
```json
{
  "email": "user@example.com",
  "password": "YourPassword"
}
```

**Success:** `200 OK`  
**Response body:** `{ "isAdmin": true, "username": "..." }` or `{ "isAdmin": false, "username": "..." }`. Cookie `access_token` set.  
**Errors:** `401` "Invalid credentials."

---

### 3. Me (current user)

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/auth/me` |
| **Auth** | Yes |

**Request body:** None.

**Success:** `200 OK`  
**Response body:** `{ "userId": "<guid>", "email": "...", "username": "...", "isAdmin": true }`.  
**Errors:** `401` if not logged in or user not found.

---

### 4. Update my username

| | |
|---|---|
| **Method** | `PUT` |
| **Path** | `/api/auth/me` |
| **Auth** | Yes |

**Request body (JSON):**
```json
{
  "username": "new_username"
}
```

**Success:** `200 OK`  
**Response body:** `{ "username": "new_username" }`.  
**Errors:** `400` "Username cannot be empty.", "Username is already taken.", `401` not logged in.

---

### 5. Logout

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/auth/logout` |
| **Auth** | No |

**Request body:** None.

**Success:** `204 No Content`. Cookie `access_token` cleared.

---

### 6. Forgot password (request reset)

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/auth/forgot-password` |
| **Auth** | No |

**Request body (JSON):**
```json
{
  "email": "user@example.com"
}
```

**Success:** `200 OK` (empty body or generic message). Always returns 200 to avoid user enumeration.  
**Behavior:** Looks up user by email; if found, generates a secure reset token (1-hour expiry), stores it, and sends an HTML email with a reset link to the user. The token is never returned in the API response.  
**Config:** Requires `FRONTEND_URL` (or `Frontend__BaseUrl`) for the reset link base (e.g. `http://localhost:5173` or `https://yourdomain.com`). Configure SMTP in `.env` (see commented vars) for email delivery.

---

### 7. Reset password (set new password with token)

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/auth/reset-password` |
| **Auth** | No |

**Request body (JSON):**
```json
{
  "token": "<token-from-reset-email-link>",
  "newPassword": "NewSecurePassword"
}
```

**Success:** `200 OK`.  
**Behavior:** Validates the token (must exist and not be expired), updates the user's password, and invalidates the token (single use).  
**Errors:** `400` "Token and new password are required.", "Invalid or expired reset token.", "User not found."

---

### 8. List users (admin only)

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/auth/users` |
| **Auth** | Yes (admin only) |

**Request body:** None.

**Success:** `200 OK`  
**Response body:** Array of `{ "id", "email", "username", "isAdmin" }`.  
**Errors:** `403` not admin.

---

### 9. Get user by id (admin only)

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/auth/users/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the user.

**Success:** `200 OK`  
**Response body:** `{ "id", "email", "username", "isAdmin" }`.  
**Errors:** `403` not admin, `404` "User not found."

---

### 10. Edit user (admin only)

| | |
|---|---|
| **Method** | `PUT` |
| **Path** | `/api/auth/users/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the user.

**Request body (JSON):** All fields optional; only send what you want to change.
```json
{
  "username": "new_username",
  "email": "new@email.com",
  "isAdmin": true
}
```

**Success:** `200 OK`  
**Response body:** `{ "id", "email", "username", "isAdmin" }`.  
**Errors:** `400` "Username cannot be empty.", "Username is already taken.", "Email cannot be empty.", "Email is already in use.", `403` not admin, `404` "User not found."

---

## Tickets (`/ticket/TicketSubmit`)

All ticket endpoints require auth (`[Auth]`).

### 11. Create a ticket

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/ticket/TicketSubmit/submit` |
| **Auth** | Yes (any user) |

**Request body (JSON):**
```json
{
  "title": "Bug in contact form",
  "description": "Submit button does nothing.",
  "category": "Bug"
}
```

**Success:** `200 OK`  
**Response body:** `{ "message": "Ticket submitted successfully", "ticketId": "<guid>" }`  
**Errors:** `400` "Required fields cannot be null or empty", `401` invalid/missing user, `404` "User not found."

---

### 12. Reply to a ticket

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/ticket/TicketSubmit/{ticketId}/reply` |
| **Auth** | Yes (ticket owner or admin) |

**Path parameter:** `ticketId` – UUID of the ticket.

**Request body (JSON):**
```json
{
  "message": "Thanks, we will look into it."
}
```

**Success:** `200 OK`  
**Response body:** `{ "message": "Reply added successfully", "replyId": "<guid>" }`  
**Errors:** `400` "Message is required.", `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not owner/admin.

---

### 13. Get replies for a ticket

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/ticket/TicketSubmit/{ticketId}/replies` |
| **Auth** | Yes (ticket owner or admin) |

**Path parameter:** `ticketId` – UUID of the ticket.

**Request body:** None.

**Success:** `200 OK`  
**Response body:** Array of objects:
```json
[
  {
    "id": "<guid>",
    "ticketId": "<guid>",
    "userId": "<guid>",
    "userEmail": "user@example.com",
    "message": "Reply text",
    "createdAt": "2025-02-11T12:00:00Z"
  }
]
```
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not owner/admin.

---

### 14. List tickets

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/ticket/TicketSubmit/get` |
| **Auth** | Yes (admin or ticket owner) |

**Request body:** None.

- **Admin:** receives all tickets.
- **Non-admin:** receives only tickets they created (owner).

**Success:** `200 OK`  
**Response body:** Array of tickets:
```json
[
  {
    "id": "<guid>",
    "title": "string",
    "description": "string",
    "category": "string",
    "createdAt": "...",
    "updatedAt": "...",
    "isResolved": false,
    "userEmail": "user@example.com"
  }
]
```
**Errors:** `401` invalid user, `404` "User not found."

---

### 15. Get one ticket by id

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/ticket/TicketSubmit/get/{id}` |
| **Auth** | Yes (admin or ticket owner) |

**Path parameter:** `id` – UUID of the ticket.

**Request body:** None.

**Success:** `200 OK`  
**Response body:** Single ticket object:
```json
{
  "id": "<guid>",
  "title": "string",
  "description": "string",
  "category": "string",
  "createdAt": "...",
  "updatedAt": "...",
  "isResolved": false,
  "userEmail": "user@example.com"
}
```
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` "You can only view your own tickets."

---

### 16. Update a ticket

| | |
|---|---|
| **Method** | `PUT` |
| **Path** | `/ticket/TicketSubmit/update/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the ticket.

**Request body (JSON):**
```json
{
  "title": "Updated title",
  "description": "Updated description",
  "category": "Support"
}
```

**Success:** `200 OK`  
**Response body:** `{ "message": "Ticket updated successfully" }`  
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not admin.

---

### 17. Delete a ticket

| | |
|---|---|
| **Method** | `DELETE` |
| **Path** | `/ticket/TicketSubmit/delete/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the ticket.

**Request body:** None.

**Success:** `200 OK`  
**Response body:** `{ "message": "Ticket deleted successfully" }`  
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not admin.

---

### 18. Resolve a ticket

| | |
|---|---|
| **Method** | `PUT` |
| **Path** | `/ticket/TicketSubmit/resolve/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the ticket.

**Request body:** None.

**Success:** `200 OK`  
**Response body:** `{ "message": "Ticket resolved successfully" }`  
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not admin.

---

### 19. Reopen a ticket

| | |
|---|---|
| **Method** | `PUT` |
| **Path** | `/ticket/TicketSubmit/reopen/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the ticket.

**Request body:** None.

Sets the ticket from resolved to not resolved (`isResolved` = false).

**Success:** `200 OK`  
**Response body:** `{ "message": "Ticket reopened successfully" }`  
**Errors:** `401` invalid user, `404` "User not found." / "Ticket not found.", `403` not admin.

---

## Showcase (`/api/showcase`)

All showcase endpoints require auth (JWT cookie or Bearer). Admin = user with `isAdmin: true`.

### 20. List items

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/showcase` |
| **Auth** | Yes |

**Request body:** None.

- **Admin:** returns all items.
- **Non-admin:** returns only items where the current user is in `userIds` (assigned).

**Success:** `200 OK`  
**Response body:** Array of items (camelCase). Each item has `id`, `type` ("site" | "photo"), `title`, `slug`, `htmlContent` (for site), `imageUrl` (for photo), `userIds`, `createdAt`, `updatedAt`.
```json
[
  {
    "id": "uuid",
    "type": "site",
    "title": "string",
    "slug": "string",
    "htmlContent": "<h1>Hello</h1>",
    "imageUrl": null,
    "userIds": ["uuid"],
    "createdAt": "ISO 8601",
    "updatedAt": "ISO 8601"
  }
]
```
**Errors:** `401` not logged in.

---

### 21. Create item (admin only)

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/showcase` |
| **Auth** | Yes (admin only) |

**Content-Type:** `application/json`  
**Body (camelCase):**

For a **site**:
```json
{
  "type": "site",
  "title": "My demo site",
  "slug": "my-demo-site",
  "userIds": ["uuid-1", "uuid-2"],
  "htmlContent": "<h1>Hello</h1>"
}
```

For a **photo**:
```json
{
  "type": "photo",
  "title": "Wedding gallery",
  "slug": "wedding-gallery",
  "userIds": ["uuid-1"],
  "imageUrl": "https://example.com/photo.jpg"
}
```

**Rules:**  
- `type`: `"site"` or `"photo"` (lowercase).  
- `title`: required, non-empty string.  
- `slug`: optional; if missing or empty, derived from title (lowercase, spaces → hyphens). Must be unique.  
- `userIds`: required array of strings (user UUIDs).  
- `htmlContent`: sent when `type === "site"` (can be `""`).  
- `imageUrl`: sent when `type === "photo"`.

**Success:** `201 Created`  
**Response:** Created object (same shape with `id`, `createdAt`, `updatedAt`, `userIds` filled). `Location: /api/showcase/slug/{slug}`.

**Errors:** `400` with body `{ "message": "..." }` or `{ "errors": { "field": ["..."] } }` (e.g. invalid type, missing title, missing userIds, missing htmlContent for site, missing imageUrl for photo). `401` not logged in; `403` not admin.

---

### 22. Get one item by slug

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/showcase/slug/{slug}` |
| **Auth** | Yes |

**Path parameter:** `slug` – URL slug of the item.

- **Admin or assigned user:** `200` + body.
- **Not assigned:** `403 Forbid`.
- **Slug not found:** `404`.

**Success:** `200 OK`  
**Response body:** Same shape as one item in the list: `id`, `type`, `title`, `slug`, `htmlContent`, `imageUrl`, `userIds`, `createdAt`, `updatedAt`.
```json
{
  "id": "uuid",
  "type": "site",
  "title": "string",
  "slug": "string",
  "htmlContent": "<h1>Hello</h1>",
  "imageUrl": null,
  "userIds": ["uuid"],
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```
**Errors:** `401` not logged in, `403` not allowed, `404` slug not found.

---

### 23. Upload image

| | |
|---|---|
| **Method** | `POST` |
| **Path** | `/api/showcase/upload` |
| **Auth** | Yes (same as rest: cookie/session, credentials) |

**Request**

- **Content-Type:** `multipart/form-data`
- **Body:** One file field named `file` (the image file).

**Success:** `200 OK`  
**Response body:**
```json
{
  "url": "https://your-domain.com/uploads/abc123.jpg"
}
```

**Errors:** JSON with `message`:
- `400` – No file, file empty, file too large (max 5 MB), or invalid type (only images: JPEG, PNG, GIF, WebP).
- `401` – Not logged in.
- `500` – Failed to save file (with optional `detail`).

**Example:** `POST /api/showcase/upload` with `FormData` containing key `file` and the image file.

---

### 24. Delete item (admin only)

| | |
|---|---|
| **Method** | `DELETE` |
| **Path** | `/api/showcase/{id}` |
| **Auth** | Yes (admin only) |

**Path parameter:** `id` – UUID of the item.

**Success:** `204 No Content`. Assignments are removed by cascade.  
**Errors:** `401` not logged in, `403` not admin, `404` not found.

---

## Cloudflare (`/api/cloudflare`)

Backend proxies [Cloudflare GraphQL Analytics API](https://developers.cloudflare.com/analytics/graphql-api/) for the configured zone (the legacy Zone Analytics REST API is sunset). You must set **Cloudflare API token** and **Zone ID** in your environment (see below).

### 25. Get zone analytics dashboard

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/cloudflare/analytics/dashboard` |
| **Auth** | Yes |

**Query parameters (optional):**

| Parameter | Type | Description |
|-----------|------|--------------|
| `since` | ISO 8601 datetime | Start of analytics period (default: 24 hours ago). |
| `until` | ISO 8601 datetime | End of period (default: now). |
| `continuous` | bool | Ignored (kept for compatibility). GraphQL API does not use it. |

**Success:** `200 OK` with **application/json** body: raw Cloudflare **GraphQL** response. Uses `httpRequestsAdaptiveGroups` (available on **all plans including Free**). Shape:

- `data.viewer.zones[0].httpRequestsAdaptiveGroups` – array of time-bucketed groups (hourly), each with:
  - `dimensions.datetimeHour` – bucket start time (ISO 8601).
  - `count` – number of requests in that bucket.
  - `sum.visits` – visits (page views from external/direct referrer) in that bucket.
  - `sum.edgeResponseBytes` – bytes transferred in that bucket.

To get totals for the period, sum `count`, `sum.visits`, and `sum.edgeResponseBytes` across all groups in your frontend.

**Errors:** `400 Bad Request` with body `{ "error": "..." }` if Cloudflare is not configured (missing `Cloudflare__ApiToken` or `Cloudflare__ZoneId`), or if the Cloudflare API returns an error.

**Example:** `GET /api/cloudflare/analytics/dashboard?since=2025-02-17T00:00:00Z&until=2025-02-24T23:59:59Z`

### Cloudflare setup (what you need to configure)

1. **Zone ID**  
   In [Cloudflare Dashboard](https://dash.cloudflare.com/) → select your domain → **Overview** → right-hand sidebar: **API** → copy **Zone ID**.

2. **API Token**  
   - Go to [API Tokens](https://dash.cloudflare.com/profile/api-tokens) → **Create Token**.  
   - Use template **“Read all resources”** or create a custom token with at least:
     - **Zone** → **Zone** → **Read**
     - **Zone** → **Analytics** → **Read**
   - Create and copy the token (shown only once).

3. **Environment variables**  
   In your `.env` (or app configuration) set:
   - `Cloudflare__ZoneId=<your-zone-id>`
   - `Cloudflare__ApiToken=<your-api-token>`

   (Use double underscore `__` for .env; in appsettings.json use `"Cloudflare": { "ZoneId": "...", "ApiToken": "..." }`.)

Without these, calls to `/api/cloudflare/analytics/dashboard` return `400` with an error message explaining what is missing.

---

## Quick reference table

| # | Method | Path | Body | Auth |
|---|--------|------|------|------|
| 1 | POST | `/api/auth/register` | `{ "email", "password", "username"? }` | No |
| 2 | POST | `/api/auth/login` | `{ "email", "password" }` | No |
| 3 | GET | `/api/auth/me` | — | Yes |
| 4 | PUT | `/api/auth/me` | `{ "username" }` | Yes |
| 5 | POST | `/api/auth/logout` | — | No |
| 6 | POST | `/api/auth/forgot-password` | `{ "email" }` | No |
| 7 | POST | `/api/auth/reset-password` | `{ "token", "newPassword" }` | No |
| 8 | GET | `/api/auth/users` | — | Admin |
| 9 | GET | `/api/auth/users/{id}` | — | Admin |
| 10 | PUT | `/api/auth/users/{id}` | `{ "username"?,"email"?,"isAdmin"? }` | Admin |
| 11 | POST | `/ticket/TicketSubmit/submit` | `{ "title", "description", "category" }` | Yes |
| 12 | POST | `/ticket/TicketSubmit/{ticketId}/reply` | `{ "message" }` | Owner/Admin |
| 13 | GET | `/ticket/TicketSubmit/{ticketId}/replies` | — | Owner/Admin |
| 14 | GET | `/ticket/TicketSubmit/get` | — | Admin or owner |
| 15 | GET | `/ticket/TicketSubmit/get/{id}` | — | Admin or owner |
| 16 | PUT | `/ticket/TicketSubmit/update/{id}` | `{ "title", "description", "category" }` | Admin |
| 17 | DELETE | `/ticket/TicketSubmit/delete/{id}` | — | Admin |
| 18 | PUT | `/ticket/TicketSubmit/resolve/{id}` | — | Admin |
| 19 | PUT | `/ticket/TicketSubmit/reopen/{id}` | — | Admin |
| 20 | GET | `/api/showcase` | — | Yes |
| 21 | POST | `/api/showcase` | `{ "type", "title", "slug"?,"userIds", "htmlContent"?,"imageUrl"? }` | Admin |
| 22 | GET | `/api/showcase/slug/{slug}` | — | Yes (admin or assigned) |
| 23 | POST | `/api/showcase/upload` | multipart/form-data, field `file` (image) | Yes |
| 24 | DELETE | `/api/showcase/{id}` | — | Admin |
| 25 | GET | `/api/cloudflare/analytics/dashboard` | — | Yes (query: `since`, `until`, `continuous`) |