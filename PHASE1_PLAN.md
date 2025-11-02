# Phase 1: Backend MVP - Task List

## Overview
Phase 1 focuses on completing the backend API infrastructure with authentication (complete), file storage, OCR integration, and receipt processing functionality.

**Estimated Duration**: 12-17 hours of focused development
**Goal**: Working backend API where users can register, login, upload receipts, and view OCR-extracted items

**Last Updated**: 2025-11-02
**Current Status**: ~50% Complete - Authentication, infrastructure, and file storage fully implemented with tests

---

## Current Status Summary

### ✅ Completed
- [x] .NET 9 project setup with all required NuGet packages
- [x] Database schema (all entities including Phase 2 bonus entities)
- [x] EF Core migrations created (`InitialCreate`)
- [x] JWT authentication infrastructure (JwtHelper, PasswordHasher)
- [x] Auth service with registration and login
- [x] Auth endpoints (POST /api/auth/register, POST /api/auth/login)
- [x] Comprehensive auth endpoint tests (13 passing tests)
- [x] CORS configuration for frontend
- [x] Basic Program.cs configuration
- [x] Docker Compose for PostgreSQL

### ✅ Completed Phases
- Phase A: Infrastructure & Logging
- Phase B: File Upload & Storage

### 🔨 In Progress
- None

### ⏳ Pending
- Phase C: OCR Integration
- Phase D: Receipt Processing Service & Endpoints
- Phase E: Testing & Polish

---

## Phase A: Infrastructure & Logging ✅ COMPLETED
**Priority: Medium** - Foundational improvements
**Completed: 2025-11-02**

### A.1 Configure Serilog ✅
- [x] Configure Serilog in `Program.cs`:
  - Add file logging (logs/splittat-.log)
  - Add console logging with colored output
  - Configure minimum log levels
  - Add request logging middleware
- [x] Update existing services to use ILogger injection
- [x] Test logging output (console and file)
- [x] Add development startup logs showing API URLs

### A.2 Global Error Handling Middleware ✅
- [x] Create `Infrastructure/ErrorHandlingMiddleware.cs`
- [x] Create consistent error response model:
  - StatusCode (int)
  - Message (string)
  - Details (string?, only in development)
  - Timestamp (DateTime)
- [x] Handle common HTTP status codes:
  - 400 Bad Request (validation errors)
  - 401 Unauthorized
  - 404 Not Found
  - 500 Internal Server Error
- [x] Register middleware in `Program.cs`
- [x] Log all exceptions with Serilog
- [x] Test error responses

### A.3 Swagger/OpenAPI Enhancement ✅
- [x] Configure Swagger UI in `Program.cs` (upgraded from MapOpenApi)
- [x] Add JWT Bearer authentication to Swagger:
  - Add security definition
  - Add "Authorize" button in UI
- [x] Test all auth endpoints in Swagger UI with JWT token

**Key Achievements:**
- Structured logging with Serilog (console + file with daily rotation)
- Global exception handling with consistent error responses
- Full Swagger UI with JWT authentication support
- Development startup logs showing API and Swagger URLs
- All 12 auth tests still passing

---

## Phase B: File Upload & Storage ✅ COMPLETED
**Priority: High** - Blocks receipt upload functionality
**Completed: 2025-11-02**

### B.1 File Storage Service ✅
- [x] Create `Services/FileStorageService.cs`:
  - `Task<string> SaveFileAsync(IFormFile file, Guid userId)` - Returns file path/URL
  - `Task<bool> DeleteFileAsync(string filePath)` - Cleanup uploaded file
  - `bool ValidateFile(IFormFile file, out string? errorMessage)` - Validation helper
- [x] File validation logic:
  - Max file size: 10MB
  - Allowed types: image/jpeg, image/png, application/pdf
  - Check file exists and is readable
- [x] Unique filename generation using Guid
- [x] Create `wwwroot/uploads/` directory structure

### B.2 Image Processing with ImageSharp ✅
- [x] Add image optimization in FileStorageService:
  - Resize images if width > 2000px (maintain aspect ratio)
  - Compress JPEG quality to 85
  - Convert PNG to JPEG for consistency
  - Skip processing for PDF files
- [x] Test with comprehensive unit tests (11 tests)

### B.3 Static File Serving ✅
- [x] Configure static files middleware in `Program.cs`:
  - `app.UseStaticFiles()` for wwwroot
  - Static files automatically serve from `/uploads` path
- [x] Test file access via HTTP

### B.4 Register Service ✅
- [x] Add FileStorageService to DI container in `Program.cs`

**Key Achievements:**
- Complete file upload and storage system with validation
- Image optimization using ImageSharp (resize, compress, format conversion)
- Static file serving configured for uploaded receipts
- Comprehensive test suite (11 passing tests)
- All 23 tests passing (12 auth + 11 file storage)

---

## Phase C: OCR Integration (3-4 hours)
**Priority: High** - Core feature for receipt processing

### C.1 Choose OCR Provider
**Decision: Google Vision API (recommended for MVP)**
- Reasons: Better accuracy, simpler setup, good documentation
- Alternative: AWS Textract (more complex, better for structured documents)

### C.2 OCR Service Setup
- [ ] Install NuGet package: `Google.Cloud.Vision.V1`
- [ ] Create Google Cloud account (if needed)
- [ ] Enable Vision API and get API credentials (JSON key file)
- [ ] Store credentials using .NET User Secrets:
  - `dotnet user-secrets init`
  - `dotnet user-secrets set "Ocr:CredentialsPath" "/path/to/credentials.json"`
- [ ] Add OCR configuration to `appsettings.json`:
  ```json
  "Ocr": {
    "Provider": "GoogleVision",
    "CredentialsPath": ""
  }
  ```

### C.3 OCR Models
- [ ] Create `Models/OcrResult.cs`:
  - `string RawText` - Full OCR text
  - `string? MerchantName` - Extracted merchant
  - `DateTime? Date` - Extracted date
  - `decimal? Total` - Extracted total amount
  - `List<string> Items` - Raw line items
  - `float Confidence` - OCR confidence score (0-1)

### C.4 OCR Service Implementation
- [ ] Create `Services/OcrService.cs`:
  - `Task<OcrResult> ExtractTextAsync(string imagePath)`
  - Google Vision API integration
  - Error handling for API failures
  - Retry logic for transient errors (max 3 retries)
  - Log API calls and results
- [ ] Register OcrService in DI (`Program.cs`)

### C.5 Receipt Text Parser
- [ ] Create receipt parsing logic in OcrService:
  - **Merchant name**: Extract from top 3 lines (largest font/first non-empty)
  - **Date**: Regex patterns for common formats:
    - MM/DD/YYYY, DD/MM/YYYY
    - Month DD, YYYY
    - YYYY-MM-DD
  - **Line items**: Pattern matching for "item name....$X.XX"
  - **Total**: Keywords - "total", "amount due", "balance"
  - **Tax**: Keywords - "tax", "hst", "gst", "vat"
  - **Tip**: Keywords - "tip", "gratuity"
- [ ] Handle multiple receipt formats (grocery, restaurant, retail)
- [ ] Test with sample receipts

---

## Phase D: Receipt Processing Service & Endpoints (4-5 hours)
**Priority: Critical** - Main user-facing feature

### D.1 Receipt DTOs
- [ ] Create `Models/Responses/ReceiptResponse.cs`:
  - Guid Id
  - string MerchantName
  - DateTime? Date
  - decimal Total
  - decimal? Tax
  - decimal? Tip
  - string ImageUrl
  - string Status (Processing, Ready, Failed)
  - DateTime CreatedAt
  - List<ReceiptItemResponse> Items
- [ ] Create `Models/Responses/ReceiptItemResponse.cs`:
  - Guid Id
  - string Name
  - decimal Price
  - int Quantity
  - int LineNumber
- [ ] Create `Models/Requests/UpdateReceiptItemsRequest.cs`:
  - List<UpdateItemDto> Items
    - Guid Id
    - string Name
    - decimal Price
    - int Quantity

### D.2 Receipt Service Implementation
- [ ] Create `Services/ReceiptService.cs`:
  - `Task<ReceiptResponse> ProcessReceiptAsync(IFormFile file, Guid userId)`
    - Save image via FileStorageService
    - Create Receipt entity with Status="Processing"
    - Save to database
    - Call OcrService.ExtractTextAsync()
    - Parse OCR result into ReceiptItems
    - Update Receipt with parsed data
    - Update Status to "Ready" or "Failed"
    - Return ReceiptResponse
  - `Task<List<ReceiptResponse>> GetUserReceiptsAsync(Guid userId, int page = 1, int pageSize = 20)`
    - Filter by UserId
    - Order by CreatedAt descending
    - Include pagination
    - Map to ReceiptResponse DTOs
  - `Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId, Guid userId)`
    - Include ReceiptItems
    - Verify user ownership (userId matches)
    - Return null if not found or unauthorized
  - `Task<ReceiptResponse> UpdateReceiptItemsAsync(Guid receiptId, UpdateReceiptItemsRequest request, Guid userId)`
    - Verify ownership
    - Update existing items
    - Recalculate total from items
    - Return updated ReceiptResponse
  - `Task<bool> DeleteReceiptAsync(Guid receiptId, Guid userId)`
    - Verify ownership
    - Delete receipt image file
    - Delete receipt and items (cascade)
    - Return true if successful
- [ ] Register ReceiptService in DI (`Program.cs`)

### D.3 Receipt Endpoints
- [ ] Create `Endpoints/ReceiptEndpoints.cs`:
  - `POST /api/receipts` - Upload & process receipt
    - [Authorize] required
    - Accept IFormFile (multipart/form-data)
    - Extract userId from JWT claims
    - Call ReceiptService.ProcessReceiptAsync()
    - Return 201 Created with ReceiptResponse
    - Handle validation errors (file size, type)
  - `GET /api/receipts` - List user's receipts
    - [Authorize] required
    - Optional query params: page, pageSize
    - Extract userId from JWT claims
    - Return 200 OK with List<ReceiptResponse>
  - `GET /api/receipts/{id}` - Get receipt details
    - [Authorize] required
    - Verify ownership
    - Return 200 OK with ReceiptResponse
    - Return 404 if not found or unauthorized
  - `PUT /api/receipts/{id}/items` - Update receipt items
    - [Authorize] required
    - Accept UpdateReceiptItemsRequest
    - Verify ownership
    - Return 200 OK with updated ReceiptResponse
  - `DELETE /api/receipts/{id}` - Delete receipt
    - [Authorize] required
    - Verify ownership
    - Return 204 No Content on success
    - Return 404 if not found or unauthorized
- [ ] Create extension method `MapReceiptEndpoints(this WebApplication app)`
- [ ] Register endpoints in `Program.cs` with `app.MapReceiptEndpoints()`

### D.4 Authorization Helper
- [ ] Create `Infrastructure/ClaimsPrincipalExtensions.cs`:
  - `Guid GetUserId(this ClaimsPrincipal user)` - Extract userId from JWT claims
  - Throw UnauthorizedException if claim missing
- [ ] Use in all protected endpoints

---

## Phase E: Testing & Polish (2-3 hours)
**Priority: Medium** - Quality assurance

### E.1 Receipt Endpoint Tests
- [ ] Create `Splittat.API.Tests/ReceiptEndpointsTests.cs`:
  - Upload receipt (authorized user) - should return 201
  - Upload receipt (unauthorized) - should return 401
  - Upload receipt with invalid file type - should return 400
  - Upload receipt with oversized file - should return 400
  - Get receipts list (authorized) - should return 200
  - Get receipt by ID (owner) - should return 200
  - Get receipt by ID (non-owner) - should return 404
  - Update receipt items (owner) - should return 200
  - Update receipt items (non-owner) - should return 404
  - Delete receipt (owner) - should return 204
  - Delete receipt (non-owner) - should return 404
- [ ] Run all tests: `dotnet test`

### E.2 Integration Testing
- [ ] Test with sample receipt images:
  - Grocery store receipt
  - Restaurant receipt with tip
  - Retail receipt
  - Poor quality image
  - Non-receipt image (should handle gracefully)
- [ ] Verify OCR accuracy
- [ ] Verify image optimization (check file sizes)
- [ ] Test error scenarios:
  - OCR API failure
  - Database connection failure
  - File storage failure

### E.3 Docker Testing
- [ ] Start PostgreSQL container: `docker-compose up -d`
- [ ] Apply migrations: `dotnet ef database update`
- [ ] Run backend: `dotnet run`
- [ ] Test full flow end-to-end:
  - Register user
  - Login
  - Upload receipt
  - View receipts
  - Edit receipt items
  - Delete receipt
- [ ] Check database records
- [ ] Check uploaded files in wwwroot/uploads/

### E.4 Code Quality
- [ ] Add XML documentation comments to public methods
- [ ] Run code analysis (if configured)
- [ ] Review and refactor any code smells
- [ ] Ensure consistent error handling across all endpoints
- [ ] Verify logging is working correctly

---

## Deployment Checklist (End of Phase 1)

- [ ] All tests passing
- [ ] Database migrations applied
- [ ] Environment variables documented
- [ ] Sample receipts tested successfully
- [ ] API documentation complete (Swagger)
- [ ] README.md updated with setup instructions
- [ ] Code committed to Git

---

## Success Criteria for Phase 1 Backend

✅ Backend API running on localhost:5001 (HTTPS)
✅ User can register a new account
✅ User can login with email/password
✅ User receives valid JWT token
✅ User can upload a receipt image
✅ Receipt is processed via OCR
✅ Items are extracted and displayed
✅ User can view list of all their receipts
✅ User can view receipt details with items
✅ User can manually edit receipt items
✅ User can delete a receipt
✅ CORS configured for frontend (localhost:5173)
✅ Error handling works properly
✅ All tests passing
✅ Logging configured and working

---

## Notes & Decisions

### OCR Provider Choice
**Decision: Google Vision API**
- Pros: Better accuracy, simpler setup, excellent documentation
- Cons: Requires Google Cloud account (free tier available)
- Alternative considered: AWS Textract (more complex, better for forms/tables)

### Storage Strategy
**Decision: Local file storage (wwwroot/uploads/)**
- Sufficient for MVP
- Easy to migrate to cloud storage later (Azure Blob/S3)
- Add cloud storage in Phase 3 or 4

### Image Format
**Decision: Convert all images to JPEG**
- Consistent format for OCR processing
- Smaller file sizes
- Good quality at 85% compression

### Pagination
**Decision: Optional query parameters**
- Default: page=1, pageSize=20
- Allows future scalability without breaking changes

---

## Next Steps After Phase 1 Backend

Once Phase 1 backend is complete, coordinate with frontend development to:
1. Test API integration with frontend
2. Verify CORS and authentication flow
3. Test file upload from browser
4. Validate error handling and user experience

Then move to Phase 2 which focuses on:
- Implementing cost splitting functionality
- Creating split calculator service
- Building group management endpoints
- Multi-person split calculations
