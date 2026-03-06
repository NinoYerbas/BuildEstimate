# BuildEstimate Takeoff Software - Frontend

This is the web frontend for your BuildEstimate API backend. It provides a complete takeoff measurement system with blueprint viewing and cost estimation.

## Features

- **JWT Authentication** - Secure login to your BuildEstimate API
- **Blueprint Upload & Measurement** - Upload plans and measure directly on them
- **Architectural Scales** - Support for both Imperial (1/4" = 1'-0") and Metric (1:50) scales
- **Interactive Measurements** - Draw lines for linear measurements, rectangles for areas
- **Real-time Cost Calculation** - Automatic cost summaries with markup and tax
- **Project Management** - Create, edit, and manage construction projects
- **Data Persistence** - All data saved to your BuildEstimate API backend

## Prerequisites

1. **Your BuildEstimate API must be running** at `https://localhost:64319`
2. Node.js installed for running the frontend

## Configuration

The API URL is configured in `.env`:

```env
VITE_API_URL=https://localhost:64319/api/v1
```

Change this URL if your API runs on a different port or domain.

## Running the Application

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm run dev
```

3. The app will open at `http://localhost:5173`

## Using the Application

### 1. Login
- Enter your BuildEstimate API credentials
- Your JWT token is stored securely in localStorage
- All API requests include the Authorization header

### 2. Project Management
- Projects are automatically created and saved to your API
- Edit project names in real-time
- All changes sync with the backend

### 3. Blueprint Measurements
- **Upload** a blueprint image (JPG, PNG, PDF)
- **Calibrate Scale**:
  - Find a known dimension on your drawing (e.g., a 10' wall)
  - Enter that length in "Reference Length"
  - Click "Calibrate" and mark both ends on the blueprint
  - Select your drawing's architectural scale (e.g., 1/4" = 1'-0")
- **Measure**:
  - Select "Measure Line" for linear measurements (walls, pipes, etc.)
  - Select "Measure Area" for area measurements (floors, ceilings, etc.)
  - Click two points on the blueprint
  - Measurements auto-save to your takeoff list

### 4. Manual Entry
- Use the "Add Measurement" panel for manual quantities
- Choose Linear, Area, or Count measurement types
- Add unit prices for cost calculations

### 5. Cost Summary
- View real-time cost totals
- Export to CSV for use in your estimates
- Breakdown by measurement type

## API Integration

This frontend integrates with your BuildEstimate REST API:

### Endpoints Used:
- `POST /api/v1/auth/login` - User authentication
- `GET /api/v1/projects` - List projects
- `POST /api/v1/projects` - Create project
- `PUT /api/v1/projects/{id}` - Update project
- `GET /api/v1/takeoff/project/{projectId}` - Get takeoff items
- `POST /api/v1/takeoff` - Create takeoff item
- `DELETE /api/v1/takeoff/{id}` - Delete takeoff item

### Authentication Flow:
1. User logs in with credentials
2. Backend returns JWT token
3. Token stored in localStorage
4. All requests include: `Authorization: Bearer <token>`
5. Invalid/expired tokens redirect to login

## SSL Certificate Warning

Since your API uses HTTPS with a self-signed certificate at `https://localhost:64319`, your browser may show a security warning. To fix this:

### Option 1: Accept the Certificate
1. Visit `https://localhost:64319` directly in your browser
2. Click "Advanced" → "Proceed to localhost"
3. This allows your browser to trust the certificate

### Option 2: Production Certificate
For production deployment, use Let's Encrypt for a free SSL certificate (as described in your API guide).

## Data Structure

The frontend uses the same data models as your backend:

```typescript
Project {
  id: string (Guid)
  name: string
  description?: string
  clientName?: string
  createdDate: string (DateTime)
  isPrevailingWage: boolean
}

TakeoffItem {
  id: string (Guid)
  projectId: string
  name: string
  type: "linear" | "area" | "count"
  quantity: number
  unit: string
  unitPrice: number
  notes?: string
}
```

## Extending the Application

### Adding New Features:
1. API functions are in `/src/app/services/api.ts`
2. Add new endpoint functions following the existing pattern
3. Use in components with async/await and toast notifications

### Connecting to Estimates:
To integrate with your full BuildEstimate system (Estimates, LineItems, Assemblies):

```typescript
// Create an estimate from takeoff items
const estimate = await estimatesApi.create({
  projectId: currentProject.id,
  name: "Main Estimate",
  overheadPercent: 15,
  profitPercent: 10
});

// Convert takeoff items to line items
const lineItems = items.map(item => ({
  estimateId: estimate.id,
  name: item.name,
  quantity: item.quantity,
  unit: item.unit,
  unitPrice: item.unitPrice
}));

await lineItemsApi.bulkCreate(estimate.id, lineItems);
```

## Troubleshooting

### "Unauthorized" Error
- Check that your API is running at the correct URL
- Verify your username/password are correct
- Check that JWT tokens are enabled in your API

### CORS Errors
Your API needs to allow requests from the frontend origin. Add this to your API's Program.cs:

```csharp
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
```

### Blueprint Measurements Not Saving
- Verify a project is created before measuring
- Check browser console for API errors
- Ensure takeoff endpoints are working in Swagger

## Production Deployment

When deploying to production:

1. Update `.env` with your production API URL:
```env
VITE_API_URL=https://buildestimate.com/api/v1
```

2. Build the production bundle:
```bash
npm run build
```

3. Deploy the `dist` folder to your web server

## Support

This frontend connects to the BuildEstimate API you built. Refer to your API guide for:
- Endpoint documentation
- Database schema
- Authentication setup
- Deployment instructions

---

Built with React, TypeScript, Tailwind CSS, and your BuildEstimate REST API.
