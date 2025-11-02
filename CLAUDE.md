# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A mobile-first PWA for scanning receipts, extracting items via OCR, and splitting costs among multiple people or groups.

**Stack:**
- Frontend: React 19.1 + TypeScript 5.9 + Vite 7.1 + TailwindCSS 4.1
- Backend: .NET 9 Minimal API + Entity Framework Core + PostgreSQL
- OCR: Google Vision API or AWS Textract
- Storage: Azure Blob/S3/Local for receipt images

**Installed Frontend Dependencies:**
- **Core:** React 19.1.1, React DOM 19.1.1, React Router DOM 7.9.5
- **State Management:** Zustand 5.0.8, TanStack Query 5.90.6
- **HTTP Client:** Axios 1.13.1
- **Forms:** React Hook Form 7.66.0, Zod 4.1.12, @hookform/resolvers 5.2.2
- **Styling:** TailwindCSS 4.1.16 (with @tailwindcss/vite plugin)
- **Icons:** Lucide React 0.552.0
- **Build Tools:** Vite 7.1.7, TypeScript 5.9.3, SWC (via @vitejs/plugin-react-swc 4.1.0)

**Package Manager:** Yarn (v4.10.3+)

## Development Commands

### Frontend (from `frontend/` directory)
```bash
yarn dev             # Start dev server (Vite) on localhost:5173
yarn build           # Build for production (TypeScript + Vite)
yarn lint            # Run ESLint
yarn preview         # Preview production build
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

**Current Implementation Status (as of 2025-11-02):**
✅ Project setup complete with Vite + React 19 + TypeScript 5.9
✅ TailwindCSS 4.1 configured with custom theme colors (primary/secondary)
✅ Absolute imports configured (`@/` alias points to `src/`)
✅ Axios client with JWT interceptor configured
✅ TanStack Query setup with QueryClientProvider
✅ Zustand auth store with localStorage persistence
✅ React Router 7 with protected routes (ProtectedRoute component)
✅ API functions created (auth.ts, receipts.ts)
✅ TypeScript types defined for User, Auth, Receipt entities
✅ Custom useAuth hook wrapping auth mutations
✅ Placeholder pages created (Home, Login, Register, Receipts, ReceiptDetail)
⏳ UI components (Button, Input, Card, Loading) - pending
⏳ Auth forms with validation - pending
⏳ Receipt upload/list/detail components - pending
⏳ Layout components (Header, Layout) - pending

**Structure:**
- `src/pages/` - Route-level components (HomePage, LoginPage, RegisterPage, ReceiptsPage, ReceiptDetailPage)
- `src/components/` - Reusable components
  - `ui/` - Generic UI components (Button, Input, Card, Loading) - **to be created**
  - `layout/` - Layout components (Header, Layout) - **to be created**
  - `receipt/` - Receipt-specific components (ReceiptScanner, ReceiptViewer, ItemAssignment) - **to be created**
  - `ProtectedRoute.tsx` - Route guard for authenticated routes ✅
- `src/api/` - API client configuration and endpoint functions ✅
  - `client.ts` - Axios instance with JWT interceptor (401 auto-redirect)
  - `queryClient.ts` - TanStack Query configuration
  - `auth.ts` - Auth API functions (login, register)
  - `receipts.ts` - Receipt API functions (upload, getAll, getById, updateItems, delete)
- `src/hooks/` - Custom React hooks
  - `useAuth.ts` - Auth hook with login/register mutations and logout ✅
- `src/store/` - Zustand stores
  - `authStore.ts` - Auth state with localStorage persistence ✅
- `src/types/` - TypeScript type definitions ✅
  - User, LoginRequest, RegisterRequest, AuthResponse
  - Receipt, ReceiptItem, ReceiptStatus, ReceiptListItem
  - ApiError
- `src/utils/` - Utility functions (splitCalculator, formatters) - **to be created**
- `src/app.css` - TailwindCSS 4.1 theme configuration with custom colors
- `vite.config.ts` - Vite configuration with `@/` path alias and Tailwind plugin

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

### Frontend Configuration

#### TailwindCSS 4.1 Setup
The project uses **TailwindCSS v4** (latest) with the new `@theme` directive approach:

**Configuration in `src/app.css`:**
```css
@import "tailwindcss";

@theme {
  --color-primary-50: #f0f9ff;
  --color-primary-500: #0ea5e9;  /* Main brand color */
  --color-primary-600: #0284c7;  /* Hover state */
  /* ... full color scale defined */

  --color-secondary-50: #faf5ff;
  --color-secondary-500: #a855f7;
  /* ... full color scale defined */
}
```

**Key differences from Tailwind v3:**
- No `tailwind.config.js` file (configuration moved to CSS)
- Use `@theme` directive in CSS instead of JS config
- Vite plugin: `@tailwindcss/vite` instead of PostCSS setup
- Dark mode still works with `class` strategy via theme variables

**Usage in components:**
```tsx
<div className="bg-primary-500 text-white">
  <h1 className="text-primary-600 dark:text-primary-400">Hello</h1>
</div>
```

**Changing theme colors:** Simply update the CSS variables in `src/app.css` - changes apply instantly across the entire app.

#### TypeScript Path Aliases
- `@/` → `src/` (configured in `tsconfig.app.json` and `vite.config.ts`)
- Example: `import { Button } from '@/components/ui/Button'`

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
