# User API Documentation

> Base URL: `/User`

## Authentication

This API uses **JWT Bearer Token** authentication (HMAC-SHA256). Tokens are obtained via the `loginWithEmail` endpoint and must be included in the `Authorization` header as `Bearer <token>`. Endpoints marked with `[Authorize]` require a valid token. Some endpoints additionally require the authenticated user to have admin privileges (`isAdmin: true`).

## Objects

### UserInfo

Represents the full user profile returned by most endpoints.

```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [
    {
      "roleId": 1,
      "slug": "admin",
      "name": "Administrator"
    }
  ],
  "phones": [
    {
      "phone": "+5511999990000"
    }
  ],
  "addresses": [
    {
      "zipCode": "01001-000",
      "address": "Praça da Sé",
      "complement": "Apt 42",
      "neighborhood": "Sé",
      "city": "São Paulo",
      "state": "SP"
    }
  ],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-03-20T09:15:00"
}
```

| Property | Type | Description |
|----------|------|-------------|
| userId | long | Unique user identifier |
| slug | string | URL-friendly unique identifier |
| imageUrl | string | URL of the user's profile image |
| name | string | Full name of the user |
| email | string | Email address |
| hash | string | User hash identifier |
| isAdmin | bool | Whether the user has admin privileges |
| birthDate | DateTime? | Date of birth (nullable) |
| idDocument | string | Identity document number (CPF) |
| pixKey | string | PIX payment key |
| password | string | Password (only used on input, null on output) |
| status | int | User status code |
| roles | RoleInfo[] | List of roles assigned to the user |
| phones | UserPhoneInfo[] | List of phone numbers |
| addresses | UserAddressInfo[] | List of addresses |
| createAt | DateTime | Creation timestamp |
| updateAt | DateTime | Last update timestamp |

### UserInsertedInfo

DTO used when creating a new user.

```json
{
  "slug": "jane-doe",
  "imageUrl": "https://cdn.example.com/users/jane-doe.jpg",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "isAdmin": false,
  "birthDate": "1995-08-22T00:00:00",
  "idDocument": "987.654.321-00",
  "pixKey": "jane.doe@example.com",
  "password": "SecureP@ss123",
  "roles": [
    {
      "roleId": 2,
      "slug": "user",
      "name": "User"
    }
  ],
  "phones": [
    {
      "phone": "+5521988880000"
    }
  ],
  "addresses": [
    {
      "zipCode": "20040-020",
      "address": "Av. Rio Branco",
      "complement": "Sala 501",
      "neighborhood": "Centro",
      "city": "Rio de Janeiro",
      "state": "RJ"
    }
  ]
}
```

| Property | Type | Description |
|----------|------|-------------|
| slug | string | URL-friendly unique identifier |
| imageUrl | string | URL of the user's profile image |
| name | string | Full name of the user |
| email | string | Email address |
| isAdmin | bool | Whether the user has admin privileges |
| birthDate | DateTime? | Date of birth (nullable) |
| idDocument | string | Identity document number (CPF) |
| pixKey | string | PIX payment key |
| password | string | User password |
| roles | RoleInfo[] | List of roles to assign |
| phones | UserPhoneInfo[] | List of phone numbers |
| addresses | UserAddressInfo[] | List of addresses |

### LoginParam

Credentials for email-based authentication.

```json
{
  "email": "john.doe@example.com",
  "password": "SecureP@ss123"
}
```

| Property | Type | Description |
|----------|------|-------------|
| email | string | User's email address |
| password | string | User's password |

### ChangePasswordParam

Parameters for changing an authenticated user's password.

```json
{
  "oldPassword": "OldP@ss123",
  "newPassword": "NewP@ss456"
}
```

| Property | Type | Description |
|----------|------|-------------|
| oldPassword | string | Current password |
| newPassword | string | New password to set |

### ChangePasswordUsingHashParam

Parameters for resetting a password using a recovery hash.

```json
{
  "recoveryHash": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "newPassword": "NewP@ss456"
}
```

| Property | Type | Description |
|----------|------|-------------|
| recoveryHash | string | Recovery hash received via email |
| newPassword | string | New password to set |

### UserSearchParam

Parameters for paginated user search.

```json
{
  "searchTerm": "john",
  "page": 1,
  "pageSize": 10
}
```

| Property | Type | Description |
|----------|------|-------------|
| searchTerm | string | Text to search for in user records |
| page | int | Page number (default: 1) |
| pageSize | int | Number of results per page (default: 10) |

### PagedResult\<UserInfo\>

Paginated response wrapper for user search results.

```json
{
  "items": [
    {
      "userId": 1,
      "slug": "john-doe",
      "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "hash": "a1b2c3d4e5f6",
      "isAdmin": false,
      "birthDate": "1990-05-15T00:00:00",
      "idDocument": "123.456.789-00",
      "pixKey": "john.doe@example.com",
      "password": null,
      "status": 1,
      "roles": [],
      "phones": [],
      "addresses": [],
      "createAt": "2024-01-10T14:30:00",
      "updateAt": "2025-03-20T09:15:00"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

| Property | Type | Description |
|----------|------|-------------|
| items | UserInfo[] | List of users for the current page |
| page | int | Current page number |
| pageSize | int | Number of items per page |
| totalCount | int | Total number of matching users |
| totalPages | int | Total number of pages |
| hasPreviousPage | bool | Whether a previous page exists |
| hasNextPage | bool | Whether a next page exists |

### RoleInfo

Represents a user role.

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

### UserPhoneInfo

Represents a user's phone number.

```json
{
  "phone": "+5511999990000"
}
```

| Property | Type | Description |
|----------|------|-------------|
| phone | string | Phone number |

### UserAddressInfo

Represents a user's address.

```json
{
  "zipCode": "01001-000",
  "address": "Praça da Sé",
  "complement": "Apt 42",
  "neighborhood": "Sé",
  "city": "São Paulo",
  "state": "SP"
}
```

| Property | Type | Description |
|----------|------|-------------|
| zipCode | string | Postal/ZIP code |
| address | string | Street address |
| complement | string | Address complement (apt, suite, etc.) |
| neighborhood | string | Neighborhood name |
| city | string | City name |
| state | string | State abbreviation |

---

## Endpoints

### 1. Upload User Image

Uploads a profile image for the authenticated user and returns the file URL.

**Endpoint:** `POST /User/uploadImageUser`

**Authentication:** Required

**Request Body:** `multipart/form-data` with a single file field.

**Request Example:**
```http
POST /User/uploadImageUser
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data; boundary=----FormBoundary

------FormBoundary
Content-Disposition: form-data; name="file"; filename="profile.jpg"
Content-Type: image/jpeg

<binary file data>
------FormBoundary--
```

**Response Success (200):**
```json
"https://cdn.example.com/users/profile-abc123.jpg"
```

**Response Error (400):**
```json
"No file uploaded"
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

### 2. Get Current User

Returns the profile of the currently authenticated user.

**Endpoint:** `GET /User/getMe`

**Authentication:** Required

**Request Example:**
```http
GET /User/getMe
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [
    {
      "roleId": 1,
      "slug": "admin",
      "name": "Administrator"
    }
  ],
  "phones": [
    {
      "phone": "+5511999990000"
    }
  ],
  "addresses": [
    {
      "zipCode": "01001-000",
      "address": "Praça da Sé",
      "complement": "Apt 42",
      "neighborhood": "Sé",
      "city": "São Paulo",
      "state": "SP"
    }
  ],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-03-20T09:15:00"
}
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"User Not Found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 3. Get User by ID

Returns a user profile by their numeric ID.

**Endpoint:** `GET /User/getById/{userId}`

**Authentication:** Required

**Path Parameters:**
- `userId` (long, required) - The unique user identifier

**Request Example:**
```http
GET /User/getById/42
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
{
  "userId": 42,
  "slug": "maria-silva",
  "imageUrl": "https://cdn.example.com/users/maria-silva.jpg",
  "name": "Maria Silva",
  "email": "maria.silva@example.com",
  "hash": "f6e5d4c3b2a1",
  "isAdmin": false,
  "birthDate": "1988-12-03T00:00:00",
  "idDocument": "321.654.987-00",
  "pixKey": "maria.silva@example.com",
  "password": null,
  "status": 1,
  "roles": [
    {
      "roleId": 2,
      "slug": "user",
      "name": "User"
    }
  ],
  "phones": [
    {
      "phone": "+5521977770000"
    }
  ],
  "addresses": [
    {
      "zipCode": "20040-020",
      "address": "Av. Rio Branco",
      "complement": "Sala 501",
      "neighborhood": "Centro",
      "city": "Rio de Janeiro",
      "state": "RJ"
    }
  ],
  "createAt": "2024-02-15T10:00:00",
  "updateAt": "2025-01-05T16:45:00"
}
```

**Response Error (404):**
```json
"User Not Found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 4. Get User by Email

Returns a user profile by their email address.

**Endpoint:** `GET /User/getByEmail/{email}`

**Authentication:** Required

**Path Parameters:**
- `email` (string, required) - The user's email address

**Request Example:**
```http
GET /User/getByEmail/john.doe@example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [],
  "phones": [],
  "addresses": [],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-03-20T09:15:00"
}
```

**Response Error (404):**
```json
"User with email not found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 5. Get User by Slug

Returns a user profile by their URL slug. This endpoint is public (no authentication required).

**Endpoint:** `GET /User/getBySlug/{slug}`

**Authentication:** Not Required

**Path Parameters:**
- `slug` (string, required) - The user's URL-friendly identifier

**Request Example:**
```http
GET /User/getBySlug/john-doe
```

**Response Success (200):**
```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [],
  "phones": [],
  "addresses": [],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-03-20T09:15:00"
}
```

**Response Error (404):**
```json
"User with slug not found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 6. Register User

Creates a new user account. This endpoint is public (no authentication required).

**Endpoint:** `POST /User/insert`

**Authentication:** Not Required

**Request Body:**
```json
{
  "slug": "jane-doe",
  "imageUrl": "https://cdn.example.com/users/jane-doe.jpg",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "isAdmin": false,
  "birthDate": "1995-08-22T00:00:00",
  "idDocument": "987.654.321-00",
  "pixKey": "jane.doe@example.com",
  "password": "SecureP@ss123",
  "roles": [
    {
      "roleId": 2,
      "slug": "user",
      "name": "User"
    }
  ],
  "phones": [
    {
      "phone": "+5521988880000"
    }
  ],
  "addresses": [
    {
      "zipCode": "20040-020",
      "address": "Av. Rio Branco",
      "complement": "Sala 501",
      "neighborhood": "Centro",
      "city": "Rio de Janeiro",
      "state": "RJ"
    }
  ]
}
```

**Request Example:**
```http
POST /User/insert
Content-Type: application/json

{
  "slug": "jane-doe",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "password": "SecureP@ss123",
  "isAdmin": false,
  "roles": [],
  "phones": [],
  "addresses": []
}
```

**Response Success (200):**
```json
{
  "userId": 5,
  "slug": "jane-doe",
  "imageUrl": null,
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "hash": "b2c3d4e5f6a1",
  "isAdmin": false,
  "birthDate": null,
  "idDocument": null,
  "pixKey": null,
  "password": null,
  "status": 1,
  "roles": [],
  "phones": [],
  "addresses": [],
  "createAt": "2025-06-01T12:00:00",
  "updateAt": "2025-06-01T12:00:00"
}
```

**Response Error (400):**
```json
"User is empty"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 7. Update User

Updates an existing user's profile. Users can only update their own profile unless they are admins.

**Endpoint:** `POST /User/update`

**Authentication:** Required

**Request Body:**
```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe-new.jpg",
  "name": "John Doe Updated",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [
    {
      "roleId": 1,
      "slug": "admin",
      "name": "Administrator"
    }
  ],
  "phones": [
    {
      "phone": "+5511999990000"
    }
  ],
  "addresses": [
    {
      "zipCode": "01001-000",
      "address": "Praça da Sé",
      "complement": "Apt 42",
      "neighborhood": "Sé",
      "city": "São Paulo",
      "state": "SP"
    }
  ],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-03-20T09:15:00"
}
```

**Request Example:**
```http
POST /User/update
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "userId": 1,
  "name": "John Doe Updated",
  "email": "john.doe@example.com",
  "slug": "john-doe"
}
```

**Response Success (200):**
```json
{
  "userId": 1,
  "slug": "john-doe",
  "imageUrl": "https://cdn.example.com/users/john-doe-new.jpg",
  "name": "John Doe Updated",
  "email": "john.doe@example.com",
  "hash": "a1b2c3d4e5f6",
  "isAdmin": false,
  "birthDate": "1990-05-15T00:00:00",
  "idDocument": "123.456.789-00",
  "pixKey": "john.doe@example.com",
  "password": null,
  "status": 1,
  "roles": [],
  "phones": [],
  "addresses": [],
  "createAt": "2024-01-10T14:30:00",
  "updateAt": "2025-06-01T12:00:00"
}
```

**Response Error (400):**
```json
"User is empty"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (403):**
```json
"Only can update your user"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 8. Login with Email

Authenticates a user with email and password. Returns a JWT token and the user profile.

**Endpoint:** `POST /User/loginWithEmail`

**Authentication:** Not Required

**Request Headers:**
- `X-Device-Fingerprint` (string, optional) - Device fingerprint for session tracking
- `User-Agent` (string, optional) - Browser/client user agent

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "SecureP@ss123"
}
```

**Request Example:**
```http
POST /User/loginWithEmail
Content-Type: application/json
X-Device-Fingerprint: fp_abc123def456
User-Agent: Mozilla/5.0

{
  "email": "john.doe@example.com",
  "password": "SecureP@ss123"
}
```

**Response Success (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIxIiwiZW1haWwiOiJqb2huLmRvZUBleGFtcGxlLmNvbSIsImlzQWRtaW4iOiJmYWxzZSJ9.abc123",
  "user": {
    "userId": 1,
    "slug": "john-doe",
    "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "hash": "a1b2c3d4e5f6",
    "isAdmin": false,
    "birthDate": "1990-05-15T00:00:00",
    "idDocument": "123.456.789-00",
    "pixKey": "john.doe@example.com",
    "password": null,
    "status": 1,
    "roles": [
      {
        "roleId": 1,
        "slug": "admin",
        "name": "Administrator"
      }
    ],
    "phones": [
      {
        "phone": "+5511999990000"
      }
    ],
    "addresses": [
      {
        "zipCode": "01001-000",
        "address": "Praça da Sé",
        "complement": "Apt 42",
        "neighborhood": "Sé",
        "city": "São Paulo",
        "state": "SP"
      }
    ],
    "createAt": "2024-01-10T14:30:00",
    "updateAt": "2025-03-20T09:15:00"
  }
}
```

**Response Error (401):**
```json
"Email or password is wrong"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 9. Check if User Has Password

Returns whether the authenticated user has a password set.

**Endpoint:** `GET /User/hasPassword`

**Authentication:** Required

**Request Example:**
```http
GET /User/hasPassword
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
true
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"User Not Found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 10. Change Password

Changes the authenticated user's password by providing the old and new passwords.

**Endpoint:** `POST /User/changePassword`

**Authentication:** Required

**Request Body:**
```json
{
  "oldPassword": "OldP@ss123",
  "newPassword": "NewP@ss456"
}
```

**Request Example:**
```http
POST /User/changePassword
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "oldPassword": "OldP@ss123",
  "newPassword": "NewP@ss456"
}
```

**Response Success (200):**
```json
"Password changed successfully"
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (404):**
```json
"User Not Found"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 11. Send Recovery Email

Sends a password recovery email to the specified email address. This endpoint is public.

**Endpoint:** `GET /User/sendRecoveryMail/{email}`

**Authentication:** Not Required

**Path Parameters:**
- `email` (string, required) - The email address to send the recovery link to

**Request Example:**
```http
GET /User/sendRecoveryMail/john.doe@example.com
```

**Response Success (200):**
```json
"Recovery email sent successfully"
```

**Response Error (404):**
```json
"Email not exist"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 12. Change Password Using Recovery Hash

Resets a user's password using a recovery hash received via email. This endpoint is public.

**Endpoint:** `POST /User/changePasswordUsingHash`

**Authentication:** Not Required

**Request Body:**
```json
{
  "recoveryHash": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "newPassword": "NewP@ss456"
}
```

**Request Example:**
```http
POST /User/changePasswordUsingHash
Content-Type: application/json

{
  "recoveryHash": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "newPassword": "NewP@ss456"
}
```

**Response Success (200):**
```json
"Password changed successfully"
```

**Response Error (500):**
```json
"Error message details"
```

---

### 13. List All Users

Returns all users in the system. Requires admin privileges.

**Endpoint:** `GET /User/list`

**Authentication:** Required (Admin only)

**Request Example:**
```http
GET /User/list
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Success (200):**
```json
[
  {
    "userId": 1,
    "slug": "john-doe",
    "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "hash": "a1b2c3d4e5f6",
    "isAdmin": true,
    "birthDate": "1990-05-15T00:00:00",
    "idDocument": "123.456.789-00",
    "pixKey": "john.doe@example.com",
    "password": null,
    "status": 1,
    "roles": [
      {
        "roleId": 1,
        "slug": "admin",
        "name": "Administrator"
      }
    ],
    "phones": [
      {
        "phone": "+5511999990000"
      }
    ],
    "addresses": [
      {
        "zipCode": "01001-000",
        "address": "Praça da Sé",
        "complement": "Apt 42",
        "neighborhood": "Sé",
        "city": "São Paulo",
        "state": "SP"
      }
    ],
    "createAt": "2024-01-10T14:30:00",
    "updateAt": "2025-03-20T09:15:00"
  },
  {
    "userId": 2,
    "slug": "maria-silva",
    "imageUrl": null,
    "name": "Maria Silva",
    "email": "maria.silva@example.com",
    "hash": "f6e5d4c3b2a1",
    "isAdmin": false,
    "birthDate": "1988-12-03T00:00:00",
    "idDocument": "321.654.987-00",
    "pixKey": null,
    "password": null,
    "status": 1,
    "roles": [
      {
        "roleId": 2,
        "slug": "user",
        "name": "User"
      }
    ],
    "phones": [],
    "addresses": [],
    "createAt": "2024-02-15T10:00:00",
    "updateAt": "2025-01-05T16:45:00"
  }
]
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

### 14. Search Users

Searches users with pagination. Requires admin privileges.

**Endpoint:** `POST /User/search`

**Authentication:** Required (Admin only)

**Request Body:**
```json
{
  "searchTerm": "john",
  "page": 1,
  "pageSize": 10
}
```

**Request Example:**
```http
POST /User/search
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "searchTerm": "john",
  "page": 1,
  "pageSize": 10
}
```

**Response Success (200):**
```json
{
  "items": [
    {
      "userId": 1,
      "slug": "john-doe",
      "imageUrl": "https://cdn.example.com/users/john-doe.jpg",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "hash": "a1b2c3d4e5f6",
      "isAdmin": false,
      "birthDate": "1990-05-15T00:00:00",
      "idDocument": "123.456.789-00",
      "pixKey": "john.doe@example.com",
      "password": null,
      "status": 1,
      "roles": [
        {
          "roleId": 2,
          "slug": "user",
          "name": "User"
        }
      ],
      "phones": [
        {
          "phone": "+5511999990000"
        }
      ],
      "addresses": [
        {
          "zipCode": "01001-000",
          "address": "Praça da Sé",
          "complement": "Apt 42",
          "neighborhood": "Sé",
          "city": "São Paulo",
          "state": "SP"
        }
      ],
      "createAt": "2024-01-10T14:30:00",
      "updateAt": "2025-03-20T09:15:00"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Response Error (401):**
```json
"Not Authorized"
```

**Response Error (500):**
```json
"Error message details"
```
