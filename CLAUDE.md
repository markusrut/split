# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A mobile-first PWA for scanning receipts, extracting items via OCR, and splitting costs among multiple people or groups.

**Stack:**
- Frontend: React 19 + TypeScript + Vite + TailwindCSS
- Backend: .NET 9 Minimal API + Entity Framework Core + PostgreSQL
- OCR: Google Vision API or AWS Textract
- Storage: Azure Blob/S3/Local for receipt images

## Development Commands

### Frontend (from `frontend/` directory)
```bash
npm run dev          # Start dev server (Vite) on localhost:5173
npm run build        # Build for production (TypeScript + Vite)
npm run lint         # Run ESLint
npm run preview      # Preview production build
```

### Backend (from `backend/Splittat.API/` directory)
```bash
dotnet run                                    # Run API (default: https://localhost:5001)
dotnet watch run                              # Run with hot reload
dotnet build                                  # Build project
dotnet ef migrations add <MigrationName>      # Create new EF Core migration
dotnet ef database update                     # Apply migrations to database
dotnet test                                   # Run unit tests (when available)
```

### Database Setup
```bash
# From project root
docker-compose up -d                          # Start PostgreSQL container

# From backend/Splittat.API/
dotnet ef migrations add InitialCreate        # First migration
dotnet ef database update                     # Apply to database
```

## Architecture & Key Concepts

### Backend Architecture (.NET Minimal API)
The backend follows a lightweight service-oriented architecture using .NET Minimal APIs:

**Structure:**
- `Program.cs` - App configuration, DI registration, middleware pipeline
- `Endpoints/` - Minimal API endpoint definitions (AuthEndpoints, ReceiptEndpoints, SplitEndpoints, GroupEndpoints)
- `Services/` - Business logic (AuthService, ReceiptService, OcrService, FileStorageService, SplitCalculator)
- `Data/` - EF Core DbContext and entity models
  - `Entities/` - Database models (User, Receipt, ReceiptItem, Group, GroupMember, Split, ItemAssignment)
  - `Migrations/` - EF Core migrations (Code First approach)
- `Models/` - DTOs for API requests/responses
  - `Requests/` - API request models
  - `Responses/` - API response models
- `Infrastructure/` - Cross-cutting concerns (JwtHelper, PasswordHasher, extensions)

**Key Design Patterns:**
- **Minimal APIs**: Endpoints defined as lambdas/static methods with route builders
- **Service Layer**: Business logic separated from endpoints for testability
- **Repository Pattern via EF Core**: DbContext provides abstraction over data access
- **Code First Migrations**: Database schema managed through C# entity models

### Frontend Architecture (React 19 + TypeScript)

**Structure:**
- `src/pages/` - Route-level components (HomePage, LoginPage, ReceiptsPage, etc.)
- `src/components/` - Reusable components
  - `ui/` - Generic UI components (Button, Input, Card, Loading)
  - `layout/` - Layout components (Header, Layout)
  - `receipt/` - Receipt-specific components (ReceiptScanner, ReceiptViewer, ItemAssignment)
- `src/api/` - API client configuration and endpoint functions
  - `client.ts` - Axios instance with interceptors for JWT
  - `auth.ts`, `receipts.ts`, `groups.ts` - Typed API functions
- `src/hooks/` - Custom React hooks (useAuth, useReceipts, useCamera)
- `src/store/` - Zustand stores for client state (authStore)
- `src/types/` - TypeScript type definitions
- `src/utils/` - Utility functions (splitCalculator, formatters)

**State Management Strategy:**
- **TanStack Query**: Server state (API data, caching, refetching)
- **Zustand**: Client state (auth token, user info persisted to localStorage)
- **React Hook Form + Zod**: Form state and validation

**Key Architectural Decisions:**
- **Mobile-First**: All UI designed for mobile with responsive enhancements
- **PWA Capabilities**: Service worker for offline support, installable (Phase 3)
- **JWT Authentication**: Token stored in localStorage, injected via Axios interceptor

### Database Schema (Key Entities)

**Core Entity Relationships:**
```
User (1) ----< (many) Receipt (1) ----< (many) ReceiptItem
User (1) ----< (many) Group (1) ----< (many) GroupMember >---- (1) User
Receipt (1) ----< (many) Split (1) ----< (many) ItemAssignment >---- (1) ReceiptItem
                                                                >---- (1) User
```

**Important Fields:**
- `Receipt.Status`: Enum - `Processing`, `Ready`, `Failed` (tracks OCR state)
- `Split.SplitType`: Enum - `Equal`, `ByItem`, `Percentage`, `Custom`
- `ItemAssignment.Percentage`: Decimal for partial item assignments (e.g., splitting one item 50/50)
- `ItemAssignment.Amount`: Calculated amount per person after split logic

### OCR Processing Flow

1. User uploads receipt image (POST /api/receipts)
2. Backend saves image to storage (FileStorageService)
3. OcrService sends image to cloud provider (Google Vision/AWS Textract)
4. Raw OCR text is parsed to extract:
   - Merchant name (top of receipt)
   - Date (regex patterns)
   - Line items (name + price patterns)
   - Total, tax, tip (keyword matching)
5. Receipt entity created with Status="Processing", then updated to "Ready"/"Failed"
6. Frontend displays parsed items, allows manual editing

### Cost Splitting Logic

**Split Types:**
1. **Equal Split**: Total ÷ N people (simplest)
2. **By Item**: Each person assigned specific items (most accurate)
3. **Percentage**: Custom percentage per person
4. **Custom**: Manual amounts

**Tax & Tip Distribution:**
- Distributed proportionally based on item assignments
- Formula: `PersonTax = (PersonItemsTotal / ReceiptSubtotal) × TotalTax`

**Multi-Person Items:**
- Single ReceiptItem can have multiple ItemAssignments
- `ItemAssignment.Percentage` determines split (e.g., 0.5 for 50%)

### Authentication & Security

- **JWT Bearer Tokens**: Generated on login, expires after configured time
- **Password Hashing**: BCrypt or ASP.NET Core Identity PasswordHasher
- **Token Storage**: Frontend stores JWT in localStorage, adds to Authorization header
- **Protected Endpoints**: Use `[Authorize]` or check claims in Minimal API endpoints
- **CORS**: Configured to allow frontend origin (localhost:5173 in dev)

## Development Workflow

### Adding New Features

1. **Backend-First Approach:**
   - Create/modify entities in `Data/Entities/`
   - Create EF migration: `dotnet ef migrations add FeatureName`
   - Apply migration: `dotnet ef database update`
   - Implement service logic in `Services/`
   - Create DTOs in `Models/Requests` and `Models/Responses`
   - Add endpoints in `Endpoints/` (register in Program.cs)
   - Test with Swagger UI or HTTP client

2. **Frontend Integration:**
   - Add TypeScript types in `src/types/`
   - Create API functions in `src/api/`
   - Create custom hook if needed (with TanStack Query)
   - Build UI components
   - Wire up to pages/routes

### Working with EF Core Migrations

- Always review generated migrations before applying
- Test rollback: `dotnet ef database update PreviousMigrationName`
- For major schema changes, consider data migration scripts
- Keep entities in sync with DTOs (use separate models for API)

### Testing Receipt Processing

Use sample receipt images from common formats:
- Grocery stores (itemized lists)
- Restaurants (with tax, tip)
- Retail (with product codes)

Test edge cases:
- Poor image quality
- Handwritten receipts
- Multiple languages
- Missing totals/dates

## Development Phases

**Current Status:** Phase 1 (MVP Foundation)

**Phase 1 Focus:**
- Basic auth (register/login)
- Receipt upload and OCR processing
- View receipt list and details
- Manual item editing

**Phase 2:** Split creation, item assignment UI, group management
**Phase 3:** PWA features (offline, installable)
**Phase 4:** Production deployment, CI/CD
**Phase 5:** Enhancements (payments, notifications, sharing)

See [PHASE1_TODOS.md](PHASE1_TODOS.md) for detailed Phase 1 task breakdown.

## Configuration & Environment

### Backend Environment Variables
In `appsettings.json` or `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=splittat;Username=postgres;Password=..."
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "Splittat.API",
    "Audience": "Splittat.Frontend",
    "ExpirationMinutes": 60
  },
  "Ocr": {
    "Provider": "GoogleVision",
    "ApiKey": "..."
  },
  "Storage": {
    "Type": "Local",
    "Path": "wwwroot/uploads"
  }
}
```

### Frontend Environment Variables
Create `.env.local`:
```
VITE_API_BASE_URL=https://localhost:5001/api
```

## Common Gotchas

- **.NET 9 (not .NET 8)**: Project uses .NET 9 (`net9.0` in csproj)
- **React 19**: Uses latest React - check for breaking changes from React 18
- **CORS**: Ensure backend CORS policy includes frontend dev server URL
- **JWT Expiration**: Handle token refresh or re-login on 401 responses
- **Image Size**: Optimize large images before sending to OCR (use ImageSharp)
- **OCR Accuracy**: Low-quality images yield poor results - consider image preprocessing
- **Decimal Precision**: Use `decimal` type for money (never `float` or `double`)
- **Timezones**: Store UTC in database, convert to local time in UI
- **File Upload Limits**: Configure max request body size for large images

## Project Goals

**Primary Goal:** Enable users to quickly split bills by taking a photo of a receipt

**Design Principles:**
- Mobile-first, touch-friendly UI
- Minimize manual data entry (OCR automation)
- Flexible splitting (equal, by item, custom)
- Clean, maintainable codebase
- Progressive enhancement (works offline when possible)

**Non-Goals (for MVP):**
- Payment processing integration (Phase 5)
- Social features (Phase 5)
- Native mobile apps (use PWA, optional Capacitor later)
