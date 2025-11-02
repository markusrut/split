# Phase 1: MVP Foundation - Task List

## Overview
Phase 1 focuses on setting up the foundational infrastructure for both frontend and backend, implementing basic authentication, and creating the core receipt upload and processing functionality.

**Estimated Duration**: 1-2 weeks
**Goal**: Working MVP where users can register, login, upload receipts, and view OCR-extracted items

**Last Updated**: 2025-11-02
**Current Status**: ~15% Complete - Infrastructure setup done, starting authentication implementation

---

## Backend Tasks

### 1. Project Setup & Configuration

#### 1.1 Initialize .NET 9 Backend Project (Note: Using .NET 9, not .NET 8)
- [x] Create `backend` directory
- [x] Initialize .NET 9 Web API project with minimal API template
- [x] Create solution file for better organization
- [x] Setup `.gitignore` for .NET projects
- [x] Install required NuGet packages:
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.4)
  - `Microsoft.EntityFrameworkCore.Design` (9.0.10)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.10)
  - `SixLabors.ImageSharp` (3.1.12)
  - `Serilog.AspNetCore` (9.0.0)
  - `Swashbuckle.AspNetCore` (9.0.6)

#### 1.2 Project Structure Setup
- [x] Create folder structure:
  - `Endpoints/` (empty)
  - `Services/` (empty)
  - `Data/Entities/` (complete with all entities)
  - `Models/Requests/` (empty)
  - `Models/Responses/` (empty)
  - `Infrastructure/` (empty, needs Extension subdirectory)
- [x] Configure `Program.cs` with basic service registration (CORS, DbContext)
- [x] Setup `appsettings.json` and `appsettings.Development.json`

#### 1.3 Configure Logging
- [ ] Setup Serilog for structured logging
- [ ] Configure console and file logging
- [ ] Add request logging middleware

---

### 2. Database Setup (EF Core Code First)

#### 2.1 Create Database Context
- [x] Create `AppDbContext.cs` in `Data/`
- [x] Configure DbContext options in `Program.cs`
- [x] Add connection string to `appsettings.json`

#### 2.2 Create User Entity
- [x] Create `User.cs` entity with all required properties
- [x] Configure entity relationships in `AppDbContext`
- [x] Add unique index on Email

#### 2.3 Create Receipt Entities
- [x] Create `Receipt.cs` entity with all fields including Status enum
- [x] Create `ReceiptItem.cs` entity with all fields
- [x] Configure one-to-many relationship (Receipt → ReceiptItems)
- [x] Add indexes for performance
- [x] **BONUS**: Phase 2 entities already created (Group, GroupMember, Split, ItemAssignment)

#### 2.4 Database Migrations
- [x] Install EF Core CLI tools (assumed installed globally)
- [x] Create initial migration (`InitialCreate`)
- [x] Review generated migration
- [ ] Apply migration to database (`dotnet ef database update`)
- [ ] Test migration rollback capability

---

### 3. Authentication System

#### 3.1 JWT Infrastructure
- [ ] Create `JwtHelper.cs` in `Infrastructure/`:
  - Token generation method
  - Token validation method
  - Extract claims method
- [ ] Add JWT settings to `appsettings.json`:
  - Secret key (min 32 characters)
  - Issuer
  - Audience
  - Expiration time
- [ ] Configure JWT authentication in `Program.cs`

#### 3.2 Password Hashing
- [ ] Create `PasswordHasher.cs` in `Infrastructure/`:
  - Hash password method (using BCrypt or built-in ASP.NET Core Identity)
  - Verify password method

#### 3.3 Auth Service
- [ ] Create `AuthService.cs` in `Services/`:
  - `RegisterAsync(RegisterRequest)` method
  - `LoginAsync(LoginRequest)` method
  - Email validation
  - Duplicate email check
  - Password validation

#### 3.4 Auth DTOs
- [ ] Create `LoginRequest.cs`:
  - Email
  - Password
- [ ] Create `RegisterRequest.cs`:
  - Email
  - Password
  - FirstName
  - LastName
- [ ] Create `AuthResponse.cs`:
  - Token (JWT)
  - UserId
  - Email
  - ExpiresAt

#### 3.5 Auth Endpoints
- [ ] Create `AuthEndpoints.cs` in `Endpoints/`
- [ ] Implement `POST /api/auth/register` endpoint
- [ ] Implement `POST /api/auth/login` endpoint
- [ ] Add input validation
- [ ] Add error handling
- [ ] Test with Swagger/Postman

---

### 4. File Upload & Storage

#### 4.1 File Storage Service
- [ ] Create `FileStorageService.cs` in `Services/`
- [ ] Implement local file storage:
  - Create `wwwroot/uploads/` directory
  - Save uploaded files with unique names (Guid)
  - Return file URL/path
- [ ] Add file validation:
  - Check file size (max 10MB)
  - Check file type (image only: jpg, png, pdf)
  - Validate file exists
- [ ] Configure static file serving in `Program.cs`

#### 4.2 Image Processing
- [ ] Add ImageSharp image optimization:
  - Resize large images (max 2000px width)
  - Compress to reduce file size
  - Convert to standard format (JPEG)

---

### 5. OCR Integration

#### 5.1 OCR Service Setup
- [ ] Choose OCR provider (Google Vision API or AWS Textract)
- [ ] Create cloud account and get API credentials
- [ ] Install OCR SDK package
- [ ] Store credentials in `appsettings.json` or user secrets

#### 5.2 OCR Service Implementation
- [ ] Create `OcrService.cs` in `Services/`
- [ ] Create `OcrResult.cs` model:
  - RawText (string)
  - MerchantName (string?)
  - Date (DateTime?)
  - Total (decimal?)
  - Items (List<string>)
  - Confidence (float)
- [ ] Implement `ExtractTextAsync(string imagePath)` method
- [ ] Handle OCR API errors gracefully
- [ ] Add retry logic for transient failures

#### 5.3 Receipt Parsing Logic
- [ ] Create receipt text parser:
  - Extract merchant name (top of receipt)
  - Extract date (regex patterns)
  - Extract total amount (keywords: "total", "amount due")
  - Extract line items (name + price patterns)
  - Extract tax and tip if present
- [ ] Handle multiple receipt formats
- [ ] Add confidence scoring

---

### 6. Receipt Processing

#### 6.1 Receipt Service
- [ ] Create `ReceiptService.cs` in `Services/`
- [ ] Implement `ProcessReceiptAsync(IFormFile, userId)`:
  - Save image to storage
  - Call OCR service
  - Parse OCR result
  - Create Receipt entity
  - Create ReceiptItem entities
  - Save to database
  - Return ReceiptResponse
- [ ] Implement `GetUserReceiptsAsync(userId)`
- [ ] Implement `GetReceiptByIdAsync(receiptId)`
- [ ] Implement `UpdateReceiptItemsAsync(receiptId, items)`
- [ ] Implement `DeleteReceiptAsync(receiptId)`

#### 6.2 Receipt DTOs
- [ ] Create `ReceiptResponse.cs`:
  - Id, MerchantName, Date, Total, Tax, Tip
  - ImageUrl, Status, CreatedAt
  - Items (List<ReceiptItemResponse>)
- [ ] Create `ReceiptItemResponse.cs`:
  - Id, Name, Price, Quantity, LineNumber
- [ ] Create `UpdateReceiptItemsRequest.cs`:
  - Items (List with Id, Name, Price, Quantity)

#### 6.3 Receipt Endpoints
- [ ] Create `ReceiptEndpoints.cs` in `Endpoints/`
- [ ] Implement `POST /api/receipts` (upload & process)
  - Accept multipart/form-data
  - Validate file
  - Require authentication
  - Return 201 Created with receipt data
- [ ] Implement `GET /api/receipts` (list user receipts)
  - Filter by current user
  - Order by CreatedAt descending
  - Optional pagination
- [ ] Implement `GET /api/receipts/{id}`
  - Include receipt items
  - Verify user owns receipt
- [ ] Implement `PUT /api/receipts/{id}/items` (manual editing)
- [ ] Implement `DELETE /api/receipts/{id}`
- [ ] Test all endpoints

---

### 7. API Configuration

#### 7.1 CORS Setup
- [x] Configure CORS in `Program.cs`:
  - Allow localhost:5173 (Vite dev server)
  - Allow credentials
  - Allow all headers and methods
- [ ] Test CORS with frontend (pending frontend implementation)

#### 7.2 Error Handling
- [ ] Create global exception handler middleware
- [ ] Return consistent error response format:
  - Status code
  - Error message
  - Error details (dev only)
  - Timestamp
- [ ] Handle common errors:
  - 400 Bad Request (validation)
  - 401 Unauthorized
  - 404 Not Found
  - 500 Internal Server Error

#### 7.3 Swagger/OpenAPI
- [ ] Configure Swagger UI
- [ ] Add XML documentation
- [ ] Add JWT authentication to Swagger
- [ ] Test all endpoints in Swagger

---

### 8. Docker Setup

#### 8.1 Docker Compose for PostgreSQL
- [x] Create `docker-compose.yml` in project root with PostgreSQL service
- [x] Configure port 5432, volumes, environment variables, and health check
- [ ] Test database connection (need to start Docker and apply migrations)
- [x] Document connection string in appsettings.json

#### 8.2 Backend Dockerfile (Optional for Phase 1)
- [ ] Create `Dockerfile` for .NET API
- [ ] Add to docker-compose if desired

---

## Frontend Tasks

### 9. Project Setup

#### 9.1 Initialize React Project
- [x] Create `frontend` directory
- [x] Initialize Vite + React 19 + TypeScript
- [x] Install and run dev server to verify setup
- [ ] Clean up default Vite template files (still showing counter template)

#### 9.2 Install Dependencies
- [ ] Install core dependencies:
  ```bash
  npm install react-router-dom @tanstack/react-query zustand axios
  ```
- [ ] Install dev dependencies:
  ```bash
  npm install -D tailwindcss postcss autoprefixer
  ```
- [ ] Install UI libraries:
  ```bash
  npm install lucide-react
  npm install react-hook-form @hookform/resolvers zod
  ```

#### 9.3 TailwindCSS Setup
- [ ] Initialize Tailwind:
  ```bash
  npx tailwindcss init -p
  ```
- [ ] Configure `tailwind.config.js` with content paths
- [ ] Add Tailwind directives to `index.css`
- [ ] Test with simple Tailwind classes

---

### 10. Project Structure

#### 10.1 Create Folder Structure
- [ ] Create folders:
  - `src/components/ui/`
  - `src/components/layout/`
  - `src/components/receipt/`
  - `src/pages/`
  - `src/api/`
  - `src/hooks/`
  - `src/store/`
  - `src/types/`
  - `src/utils/`
- [ ] Create `index.ts` barrel exports where needed

#### 10.2 TypeScript Configuration
- [ ] Configure `tsconfig.json` for absolute imports
- [ ] Add path aliases (@/ for src/)
- [ ] Enable strict mode

---

### 11. API Client Setup

#### 11.1 Axios Configuration
- [ ] Create `src/api/client.ts`:
  - Configure base URL (localhost:5000 for dev)
  - Add request interceptor for JWT token
  - Add response interceptor for error handling
  - Export axios instance

#### 11.2 TanStack Query Setup
- [ ] Create `src/api/queryClient.ts`
- [ ] Configure QueryClient with defaults
- [ ] Wrap app with QueryClientProvider in `main.tsx`

#### 11.3 API Type Definitions
- [ ] Create `src/types/index.ts`:
  - User type
  - LoginRequest, RegisterRequest types
  - AuthResponse type
  - Receipt, ReceiptItem types
  - ReceiptResponse type
  - ApiError type

#### 11.4 Auth API
- [ ] Create `src/api/auth.ts`:
  - `login(email, password)` function
  - `register(data)` function
  - Return typed responses

#### 11.5 Receipts API
- [ ] Create `src/api/receipts.ts`:
  - `uploadReceipt(file)` function
  - `getReceipts()` function
  - `getReceiptById(id)` function
  - `updateReceiptItems(id, items)` function
  - `deleteReceipt(id)` function

---

### 12. State Management

#### 12.1 Auth Store (Zustand)
- [ ] Create `src/store/authStore.ts`:
  - State: user, token, isAuthenticated
  - Actions: login, logout, setUser
  - Persist token to localStorage
  - Auto-load token on app start

#### 12.2 Auth Hook
- [ ] Create `src/hooks/useAuth.ts`:
  - Wrap auth store
  - Add login mutation (TanStack Query)
  - Add register mutation
  - Add logout function
  - Handle token expiration

---

### 13. Routing & Navigation

#### 13.1 Setup React Router
- [ ] Configure React Router in `App.tsx`:
  - BrowserRouter
  - Routes with path matching
- [ ] Create route structure:
  - `/` - Home (public)
  - `/login` - Login page
  - `/register` - Register page
  - `/receipts` - Receipts list (protected)
  - `/receipts/:id` - Receipt detail (protected)

#### 13.2 Protected Routes
- [ ] Create `ProtectedRoute` component:
  - Check authentication status
  - Redirect to login if not authenticated
  - Allow access if authenticated

---

### 14. Layout Components

#### 14.1 Main Layout
- [ ] Create `src/components/layout/Layout.tsx`:
  - Header component
  - Main content area
  - Responsive container

#### 14.2 Header
- [ ] Create `src/components/layout/Header.tsx`:
  - App logo/title
  - Navigation links
  - User menu (logout)
  - Mobile-responsive hamburger menu

---

### 15. Authentication Pages

#### 15.1 Login Page
- [ ] Create `src/pages/LoginPage.tsx`:
  - Email input
  - Password input
  - Submit button
  - Link to register page
  - Form validation with React Hook Form + Zod
  - Error display
  - Loading state
  - Redirect after successful login

#### 15.2 Register Page
- [ ] Create `src/pages/RegisterPage.tsx`:
  - First name input
  - Last name input
  - Email input
  - Password input
  - Confirm password input
  - Submit button
  - Link to login page
  - Form validation
  - Error display
  - Loading state
  - Redirect after successful registration

---

### 16. Receipt Components

#### 16.1 Receipt Scanner/Upload
- [ ] Create `src/components/receipt/ReceiptScanner.tsx`:
  - File input button
  - Camera capture (if supported)
  - Image preview before upload
  - Upload progress indicator
  - Error handling
  - Success feedback

#### 16.2 Camera Hook
- [ ] Create `src/hooks/useCamera.ts`:
  - Check camera permission
  - Capture photo from camera
  - Handle camera errors
  - Convert to file for upload

---

### 17. Receipt Pages

#### 17.1 Receipts List Page
- [ ] Create `src/pages/ReceiptsPage.tsx`:
  - Display list of user receipts
  - Show receipt card with:
    - Merchant name
    - Date
    - Total amount
    - Thumbnail
  - Click to view details
  - Upload new receipt button
  - Empty state (no receipts yet)
  - Loading skeleton
  - Error state
  - Use TanStack Query for data fetching

#### 17.2 Receipt Detail Page
- [ ] Create `src/pages/ReceiptDetailPage.tsx`:
  - Display receipt image
  - Show merchant, date, total
  - List all items with prices
  - Edit button for manual corrections
  - Delete receipt button
  - Back navigation
  - Loading state
  - Error handling

#### 17.3 Receipt Viewer Component
- [ ] Create `src/components/receipt/ReceiptViewer.tsx`:
  - Display receipt image (zoomable)
  - Show metadata
  - List items in table format
  - Responsive design

---

### 18. UI Components

#### 18.1 Reusable Components
- [ ] Create `src/components/ui/Button.tsx`:
  - Variants (primary, secondary, danger)
  - Sizes (sm, md, lg)
  - Loading state
  - Disabled state
- [ ] Create `src/components/ui/Input.tsx`:
  - Text, email, password types
  - Error state
  - Label support
- [ ] Create `src/components/ui/Card.tsx`:
  - Flexible container
  - Shadow and border styling
- [ ] Create `src/components/ui/Loading.tsx`:
  - Spinner component
  - Skeleton loader
- [ ] Create `src/components/ui/ErrorMessage.tsx`:
  - Display API errors
  - Dismissible alerts

---

### 19. Utilities

#### 19.1 Formatters
- [ ] Create `src/utils/formatters.ts`:
  - `formatCurrency(amount)` - Format as dollar amount
  - `formatDate(date)` - Format date strings
  - `formatDateTime(date)` - Format with time

#### 19.2 Validators
- [ ] Create validation schemas with Zod:
  - Email validation
  - Password requirements (min length, complexity)
  - File size/type validation

---

### 20. Testing & Integration

#### 20.1 End-to-End Testing
- [ ] Test user registration flow
- [ ] Test user login flow
- [ ] Test receipt upload with sample image
- [ ] Test receipt list display
- [ ] Test receipt detail view
- [ ] Test manual item editing
- [ ] Test receipt deletion

#### 20.2 Error Scenarios
- [ ] Test invalid login credentials
- [ ] Test duplicate email registration
- [ ] Test unauthorized access to protected routes
- [ ] Test invalid file upload
- [ ] Test OCR failure handling
- [ ] Test network errors

#### 20.3 Responsive Design
- [ ] Test on mobile viewport (375px)
- [ ] Test on tablet viewport (768px)
- [ ] Test on desktop viewport (1280px+)
- [ ] Test touch interactions
- [ ] Test camera access on mobile

---

## Documentation

### 21. README & Docs
- [ ] Create `backend/README.md`:
  - Setup instructions
  - Database migration commands
  - Environment variables
  - API endpoints documentation
- [ ] Create `frontend/README.md`:
  - Setup instructions
  - Environment variables
  - Development commands
  - Project structure overview
- [ ] Update root `README.md`:
  - Project overview
  - Quick start guide
  - Links to backend/frontend READMEs

---

## Success Criteria for Phase 1

✅ Backend API running on localhost:5000
✅ Frontend running on localhost:5173
✅ User can register a new account
✅ User can login with email/password
✅ User receives JWT token and stays logged in
✅ User can upload a receipt image
✅ Receipt is processed via OCR
✅ Items are extracted and displayed
✅ User can view list of all their receipts
✅ User can view receipt details
✅ User can manually edit receipt items
✅ User can delete a receipt
✅ Mobile-responsive design works
✅ CORS configured correctly
✅ Error handling works properly
✅ Code is in version control (Git)

---

## Next Steps After Phase 1

Once Phase 1 is complete, move to Phase 2 which focuses on:
- Implementing the cost splitting functionality
- Creating the split calculator
- Building item assignment UI
- Adding group management
- Multi-person split calculations

---

## Notes

- Focus on getting the core functionality working first
- Don't over-optimize initially - working code > perfect code
- Test each component as you build it
- Commit code frequently to Git
- Document any issues or decisions in comments
- Keep the UI simple and functional for MVP
