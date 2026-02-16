# Role API Documentation

> Base URL: `/Role`

## Authentication

This API uses **JWT Bearer Token** authentication (HMAC-SHA256). All endpoints in this controller require authentication **and** admin privileges (`isAdmin: true`). Tokens must be included in the `Authorization` header as `Bearer <token>`.

## Objects

### RoleInfo

Represents a role in the system.

```json
{
  "roleId": 1,
  "slug": "admin",
  "name": "Administrator"
}
```

| Property | Type | Description |
|----------|------|-------------|
| roleId | long | Unique role identifier |
| slug | string | URL-friendly role identifier |
| name | string | Display name of the role |

---

## Endpoints

### 1. List All Roles

Returns all roles in the system. Requires admin privileges.

**Endpoint:** `GET /Role/list`

**Authentication:** Required (Admin only)

**Request Example:**
```http
GET /Role/list
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
[
  {
    "roleId": 1,
    "slug": "admin",
    "name": "Administrator"
  },
  {
    "roleId": 2,
    "slug": "user",
    "name": "User"
  },
  {
    "roleId": 3,
    "slug": "moderator",
    "name": "Moderator"
  }
]
```

**Response Error (400):**
```json
"Validation error message"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 2. Get Role by ID

Returns a single role by its numeric ID. Requires admin privileges.

**Endpoint:** `GET /Role/getById/{roleId}`

**Authentication:** Required (Admin only)

**Path Parameters:**
- `roleId` (long, required) - The unique role identifier

**Request Example:**
```http
GET /Role/getById/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
{
  "roleId": 1,
  "slug": "admin",
  "name": "Administrator"
}
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"Role not found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 3. Get Role by Slug

Returns a single role by its URL-friendly slug. Requires admin privileges.

**Endpoint:** `GET /Role/getBySlug/{slug}`

**Authentication:** Required (Admin only)

**Path Parameters:**
- `slug` (string, required) - The role's URL-friendly identifier

**Request Example:**
```http
GET /Role/getBySlug/admin
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
{
  "roleId": 1,
  "slug": "admin",
  "name": "Administrator"
}
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"Role not found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 4. Create Role

Creates a new role. Requires admin privileges.

**Endpoint:** `POST /Role/insert`

**Authentication:** Required (Admin only)

**Request Body:**
```json
{
  "roleId": 0,
  "slug": "editor",
  "name": "Editor"
}
```

**Request Example:**
```http
POST /Role/insert
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "slug": "editor",
  "name": "Editor"
}
```

**Response Success (200):**
```json
{
  "roleId": 4,
  "slug": "editor",
  "name": "Editor"
}
```

**Response Error (400):**
```json
"Validation error message"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 5. Update Role

Updates an existing role. Requires admin privileges.

**Endpoint:** `POST /Role/update`

**Authentication:** Required (Admin only)

**Request Body:**
```json
{
  "roleId": 4,
  "slug": "editor",
  "name": "Content Editor"
}
```

**Request Example:**
```http
POST /Role/update
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "roleId": 4,
  "slug": "editor",
  "name": "Content Editor"
}
```

**Response Success (200):**
```json
{
  "roleId": 4,
  "slug": "editor",
  "name": "Content Editor"
}
```

**Response Error (400):**
```json
"Validation error message"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"Role not found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 6. Delete Role

Deletes a role by its ID. Requires admin privileges.

**Endpoint:** `DELETE /Role/delete/{roleId}`

**Authentication:** Required (Admin only)

**Path Parameters:**
- `roleId` (long, required) - The unique role identifier to delete

**Request Example:**
```http
DELETE /Role/delete/4
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
"Role deleted successfully"
```

**Response Error (400):**
```json
"Validation error message"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"Role not found"
```

**Response Error (500):**
```json
"Error message details"
```
