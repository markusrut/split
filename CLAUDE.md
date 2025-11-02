# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A mobile-first PWA for scanning receipts, extracting items via OCR, and splitting costs among multiple people or groups.

**Stack:**
- Frontend: React 19.1 + TypeScript 5.9 + Vite 7.1 + TailwindCSS 4.1
- Backend: .NET 9 Minimal API + Entity Framework Core + PostgreSQL
- OCR: Azure Computer Vision API (lowest cost, best .NET integration)
- Storage: Local (wwwroot/uploads) for MVP, Azure Blob Storage for production
- Hosting: Azure App Service (planned for production deployment)

**Installed Frontend Dependencies:**
- **Core:** React 19.1.1, React DOM 19.1.1, React Router DOM 7.9.5
- **State Management:** Zustand 5.0.8, TanStack Query 5.90.6
- **HTTP Client:** Axios 1.13.1
- **Forms:** React Hook Form 7.66.0, Zod 4.1.12, @hookform/resolvers 5.2.2
- **Styling:** TailwindCSS 4.1.16 (with @tailwindcss/vite plugin)
- **Icons:** Lucide React 0.552.0
- **Build Tools:** Vite 7.1.7, TypeScript 5.9.3, SWC (via @vitejs/plugin-react-swc 4.1.0)

**Package Manager:** Yarn (v4.10.3+)

**Code Quality Tools:**
- **Prettier 3.6.2:** Code formatting (semi, double quotes, 80 char width, 2-space tabs)
- **ESLint 9.36.0:** Linting with TypeScript, React, React Hooks rules
- **Configuration Files:**
  - `.prettierrc` - Prettier formatting rules
  - `.prettierignore` - Ignored paths (dist, .yarn, node_modules, .pnp.*, coverage)
  - `eslint.config.js` - ESLint configuration with globalIgnores and test file rules

**Testing Setup:**
- **Framework:** Vitest 4.0.6 (fast, Vite-native test runner)
- **Testing Library:** @testing-library/react 16.3.0, @testing-library/jest-dom 6.9.1
- **Test Environment:** jsdom 27.1.0
- **User Interaction:** @testing-library/user-event 14.6.1
- **Configuration:** `vitest.config.ts` with globals, jsdom environment, setup files
- **Custom Test Utilities:** `src/test/test-utils.tsx` provides custom render with QueryClient and Router providers
- **Test Location:** Adjacent to source files (e.g., `Button.tsx` → `Button.test.tsx`)
- **Current Coverage:** 51 passing tests across 6 test files
  - Button component: 6 tests
  - HomePage: 6 tests (including mobile/tablet/desktop snapshots)
  - LoginPage: 7 tests (including mobile/tablet/desktop snapshots + loading/error states)
  - RegisterPage: 8 tests (including mobile/tablet/desktop snapshots + loading/error states)
  - ReceiptsPage: 11 tests (including responsive snapshots + loading/empty/error states)
  - ReceiptDetailPage: 13 tests (including responsive snapshots + loading/error/processing states)

## Development Commands

### Frontend (from `frontend/` directory)
```bash
yarn dev             # Start dev server (Vite) on localhost:5173
yarn build           # Build for production (TypeScript + Vite)
yarn lint            # Run ESLint + TypeScript type checking
yarn preview         # Preview production build
yarn test            # Run tests in watch mode
yarn test:ui         # Run tests with Vitest UI
yarn test:run        # Run tests once (CI mode)
yarn test:coverage   # Run tests with coverage report
yarn format          # Format code with Prettier
yarn format:check    # Check code formatting without changes
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
✅ UI components (Button, Input, Card, Loading, ErrorMessage) with forwardRef pattern
✅ Auth forms with React Hook Form + Zod validation (LoginPage, RegisterPage)
✅ Layout components (Header with nav/dark mode, Layout wrapper)
✅ Dark mode implementation with Zustand persistence (useDarkMode hook)
✅ Test framework setup (Vitest + Testing Library + jsdom)
✅ Code quality tools (Prettier formatting, ESLint configured, 6 passing tests)
⏳ Receipt upload/scanner component - pending
⏳ Receipt list/detail components - pending

**Structure:**
- `src/pages/` - Route-level components
  - `HomePage.tsx` - Landing page ✅
  - `LoginPage.tsx` - Login form with React Hook Form + Zod ✅
  - `RegisterPage.tsx` - Registration form with validation ✅
  - `ReceiptsPage.tsx` - Receipt list page (placeholder) ✅
  - `ReceiptDetailPage.tsx` - Receipt detail view (placeholder) ✅
- `src/components/` - Reusable components
  - `ui/` - Generic UI components ✅
    - `Button.tsx` - Button with variants (primary, secondary, danger, outline, ghost) and loading states
    - `Input.tsx` - Input field with label, error, helper text
    - `Card.tsx` - Card container with Header, Body, Footer subcomponents
    - `Loading.tsx` - Loading spinner and Skeleton components
    - `ErrorMessage.tsx` - Alert/error display with dismissible option
  - `layout/` - Layout components ✅
    - `Header.tsx` - Navigation header with dark mode toggle and auth state
    - `Layout.tsx` - Page wrapper with Header
  - `receipt/` - Receipt-specific components (to be created)
  - `ProtectedRoute.tsx` - Route guard for authenticated routes ✅
- `src/api/` - API client configuration and endpoint functions ✅
  - `client.ts` - Axios instance with JWT interceptor (401 auto-redirect)
  - `queryClient.ts` - TanStack Query configuration
  - `auth.ts` - Auth API functions (login, register)
  - `receipts.ts` - Receipt API functions (upload, getAll, getById, updateItems, delete)
- `src/hooks/` - Custom React hooks
  - `useAuth.ts` - Auth hook with login/register mutations and logout ✅
  - `useDarkMode.ts` - Dark mode state management with Zustand + localStorage persistence ✅
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

**Component Patterns:**
- **forwardRef Pattern**: All UI components use React.forwardRef for proper ref handling
- **HTMLAttributes Extension**: Components extend appropriate HTML*Attributes types for TypeScript safety
  - Example: `Button extends ButtonHTMLAttributes<HTMLButtonElement>`
  - Benefit: Automatic support for all standard HTML props (className, onClick, aria-*, data-*, etc.)
- **Controlled Components**: Form inputs use React Hook Form's register/control for validation
- **Compound Components**: Card uses subcomponents (Card.Header, Card.Body, Card.Footer) for flexibility
- **Loading States**: Async components show Loading/Skeleton while data fetches
- **Error Boundaries**: ErrorMessage component for user-facing error display

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

### Code Quality Standards (Frontend)

**IMPORTANT: After making ANY code changes in the frontend, ALWAYS run these commands in order:**

```bash
yarn format         # Format all code with Prettier
yarn lint           # Run ESLint + TypeScript type checking
yarn test:run       # Run all tests in CI mode
```

**All three commands must pass with 0 errors/warnings before committing code.**

- `yarn format` ensures consistent code style across the codebase
- `yarn lint` runs both ESLint (for code quality) and TypeScript type checking (for type correctness)
  - Combines `eslint .` + `tsc --noEmit` into a single command
- `yarn test:run` verifies all unit tests pass

**Test-Driven Development:**
- Write tests for new components/utilities before or during implementation
- Place test files adjacent to source files: `Button.tsx` → `Button.test.tsx`
- Use custom render from `src/test/test-utils.tsx` for components that need providers
- Aim for meaningful tests that verify component behavior, not implementation details

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
   - **Run `yarn format && yarn lint && yarn test:run` before committing**

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
**Phase 4:** Production deployment, CI/CD (Azure App Service)
**Phase 5:** Enhancements (payments, notifications, sharing)

See [PHASE1_PLAN.md](PHASE1_PLAN.md) for detailed Phase 1 task breakdown and progress tracking.

## Azure Hosting Plan

### Phase 1 (MVP Development - Testing)
**Cost: $0/month**
- Azure App Service: Free F1 tier (60 CPU mins/day, 1GB RAM)
- PostgreSQL: Docker container (local or on App Service)
- Azure Computer Vision: Free F0 tier (5,000 requests/month)

### Phase 2 (Production Launch - Starter)
**Cost: ~$48/month**
- Azure App Service: Basic B1 tier (~$13/month, 1.75GB RAM, 1 core)
- Azure Database for PostgreSQL: Basic tier (~$30/month, 1 vCore, 5GB storage)
- Azure Computer Vision: Pay-as-you-go (~$5/month for 10k receipts after free tier)

### Future Scaling (Phase 4+)
**Cost: ~$225/month for 100k receipts/month**
- Azure App Service: Standard S1 with auto-scale (~$70/month)
- Azure Database for PostgreSQL: General Purpose (~$100/month)
- Azure Blob Storage: For receipt images (~$5/month)
- Azure Computer Vision: ~$50/month for 100k receipts

**Benefits of Azure ecosystem:**
- Managed Identity (no credential management needed)
- Single billing dashboard
- Excellent .NET integration
- Easy deployment: `az webapp up --name splittat-api`

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
  "Azure": {
    "ComputerVision": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "your-api-key-here"
    }
  },
  "Storage": {
    "Type": "Local",
    "Path": "wwwroot/uploads"
  }
}
```

**For development**, use .NET User Secrets to avoid committing credentials:
```bash
dotnet user-secrets init
dotnet user-secrets set "Azure:ComputerVision:Endpoint" "https://your-resource.cognitiveservices.azure.com/"
dotnet user-secrets set "Azure:ComputerVision:ApiKey" "your-api-key"
```

**For production (Azure App Service)**, use Managed Identity instead of API keys:
- Enable Managed Identity on App Service
- Grant Computer Vision permissions to the identity
- Code automatically uses `DefaultAzureCredential` (no secrets needed)

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
- **Nullable Reference Types**: ALWAYS consider if values can be null and mark with `?` appropriately (see [backend/NULLABLE_GUIDELINES.md](backend/NULLABLE_GUIDELINES.md) for comprehensive rules)

## Project Goals

@PROJECT_PLAN.md

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
