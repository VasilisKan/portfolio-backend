# Ticket endpoints

Base path: **`/ticket/TicketSubmit`**

All ticket endpoints require authentication (JWT in cookie `access_token` or Bearer header) unless noted.

---

## 1. Create a ticket

**`POST /ticket/TicketSubmit/submit`**

- **Auth:** Required (any logged-in user).
- **Body:**
  ```json
  {
    "title": "string",
    "description": "string",
    "category": "string"
  }
  ```
- **Response:** `{ "message": "Ticket submitted successfully", "ticketId": "<guid>" }`

---

## 2. Reply to a ticket

**`POST /ticket/TicketSubmit/{ticketId}/reply`**

- **Auth:** Required. Caller must be the ticket owner or an admin.
- **Body:**
  ```json
  {
    "message": "string"
  }
  ```
- **Response:** `{ "message": "Reply added successfully", "replyId": "<guid>" }`

---

## 3. Get replies for a ticket

**`GET /ticket/TicketSubmit/{ticketId}/replies`**

- **Auth:** Required. Caller must be the ticket owner or an admin.
- **Response:** Array of replies, each with `id`, `ticketId`, `userId`, `userEmail`, `message`, `createdAt`.

---

## 4. List all tickets (admin only)

**`GET /ticket/TicketSubmit/get`**

- **Auth:** Required. Admin only.
- **Response:** Array of tickets with `id`, `title`, `description`, `category`, `createdAt`, `updatedAt`, `isResolved`, `userEmail`.

---

## 5. Update a ticket (admin only)

**`PUT /ticket/TicketSubmit/update/{id}`**

- **Auth:** Required. Admin only.
- **Body:**
  ```json
  {
    "title": "string",
    "description": "string",
    "category": "string"
  }
  ```
- **Response:** `{ "message": "Ticket updated successfully" }`

---

## 6. Delete a ticket (admin only)

**`DELETE /ticket/TicketSubmit/delete/{id}`**

- **Auth:** Required. Admin only.
- **Response:** `{ "message": "Ticket deleted successfully" }`

---

## Summary table

| Method | Path | Description | Who |
|--------|------|-------------|-----|
| POST | `/ticket/TicketSubmit/submit` | Create a ticket | Any user |
| POST | `/ticket/TicketSubmit/{ticketId}/reply` | Reply to a ticket | Owner or admin |
| GET | `/ticket/TicketSubmit/{ticketId}/replies` | Get replies for a ticket | Owner or admin |
| GET | `/ticket/TicketSubmit/get` | List all tickets | Admin only |
| PUT | `/ticket/TicketSubmit/update/{id}` | Update a ticket | Admin only |
| DELETE | `/ticket/TicketSubmit/delete/{id}` | Delete a ticket | Admin only |
