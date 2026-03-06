// BuildEstimate API Service
// Base URL for the API
const API_BASE_URL = import.meta.env.VITE_API_URL || "https://localhost:64319/api/v1";

// Types matching your backend entities
export interface Project {
  id: string;
  name: string;
  description?: string;
  clientName?: string;
  createdDate: string;
  isPrevailingWage: boolean;
}

export interface Estimate {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  createdDate: string;
  totalCost: number;
  overheadPercent: number;
  profitPercent: number;
  bidPrice: number;
}

export interface LineItem {
  id: string;
  estimateId: string;
  name: string;
  description?: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  laborHours?: number;
  laborRate?: number;
  totalCost: number;
  csiDivision?: string;
}

export interface TakeoffItem {
  id: string;
  projectId: string;
  estimateId?: string;
  name: string;
  type: "linear" | "area" | "count";
  quantity: number;
  unit: string;
  unitPrice: number;
  notes?: string;
  blueprintId?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

// Auth token management
let authToken: string | null = null;

export const setAuthToken = (token: string) => {
  authToken = token;
  localStorage.setItem("buildestimate_token", token);
};

export const getAuthToken = () => {
  if (!authToken) {
    authToken = localStorage.getItem("buildestimate_token");
  }
  return authToken;
};

export const clearAuthToken = () => {
  authToken = null;
  localStorage.removeItem("buildestimate_token");
};

// Base fetch wrapper with JWT authentication
async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = getAuthToken();
  
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...options.headers,
  };

  // Add Authorization header if token exists
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      // Unauthorized - clear token and redirect to login
      clearAuthToken();
      throw new Error("Unauthorized. Please log in again.");
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: `HTTP ${response.status}: ${response.statusText}`,
      }));
      throw new Error(error.message || "API request failed");
    }

    return await response.json();
  } catch (error) {
    if (error instanceof Error) {
      throw error;
    }
    throw new Error("Network error");
  }
}

// === PROJECTS API ===
export const projectsApi = {
  getAll: () => apiFetch<Project[]>("/projects"),
  
  getById: (id: string) => apiFetch<Project>(`/projects/${id}`),
  
  create: (project: Omit<Project, "id" | "createdDate">) =>
    apiFetch<Project>("/projects", {
      method: "POST",
      body: JSON.stringify(project),
    }),
  
  update: (id: string, project: Partial<Project>) =>
    apiFetch<Project>(`/projects/${id}`, {
      method: "PUT",
      body: JSON.stringify(project),
    }),
  
  delete: (id: string) =>
    apiFetch<void>(`/projects/${id}`, {
      method: "DELETE",
    }),
};

// === ESTIMATES API ===
export const estimatesApi = {
  getByProjectId: (projectId: string) =>
    apiFetch<Estimate[]>(`/estimates/project/${projectId}`),
  
  getById: (id: string) => apiFetch<Estimate>(`/estimates/${id}`),
  
  create: (estimate: Omit<Estimate, "id" | "createdDate" | "totalCost" | "bidPrice">) =>
    apiFetch<Estimate>("/estimates", {
      method: "POST",
      body: JSON.stringify(estimate),
    }),
  
  update: (id: string, estimate: Partial<Estimate>) =>
    apiFetch<Estimate>(`/estimates/${id}`, {
      method: "PUT",
      body: JSON.stringify(estimate),
    }),
  
  delete: (id: string) =>
    apiFetch<void>(`/estimates/${id}`, {
      method: "DELETE",
    }),
  
  calculate: (id: string) =>
    apiFetch<Estimate>(`/estimates/${id}/calculate`, {
      method: "POST",
    }),
};

// === LINE ITEMS API ===
export const lineItemsApi = {
  getByEstimateId: (estimateId: string) =>
    apiFetch<LineItem[]>(`/lineitems/estimate/${estimateId}`),
  
  getById: (id: string) => apiFetch<LineItem>(`/lineitems/${id}`),
  
  create: (lineItem: Omit<LineItem, "id" | "totalCost">) =>
    apiFetch<LineItem>("/lineitems", {
      method: "POST",
      body: JSON.stringify(lineItem),
    }),
  
  update: (id: string, lineItem: Partial<LineItem>) =>
    apiFetch<LineItem>(`/lineitems/${id}`, {
      method: "PUT",
      body: JSON.stringify(lineItem),
    }),
  
  delete: (id: string) =>
    apiFetch<void>(`/lineitems/${id}`, {
      method: "DELETE",
    }),
  
  bulkCreate: (estimateId: string, lineItems: Omit<LineItem, "id" | "totalCost" | "estimateId">[]) =>
    apiFetch<LineItem[]>(`/lineitems/bulk/${estimateId}`, {
      method: "POST",
      body: JSON.stringify(lineItems),
    }),
};

// === TAKEOFF API ===
export const takeoffApi = {
  getByProjectId: (projectId: string) =>
    apiFetch<TakeoffItem[]>(`/takeoff/project/${projectId}`),
  
  getById: (id: string) => apiFetch<TakeoffItem>(`/takeoff/${id}`),
  
  create: (takeoffItem: Omit<TakeoffItem, "id">) =>
    apiFetch<TakeoffItem>("/takeoff", {
      method: "POST",
      body: JSON.stringify(takeoffItem),
    }),
  
  update: (id: string, takeoffItem: Partial<TakeoffItem>) =>
    apiFetch<TakeoffItem>(`/takeoff/${id}`, {
      method: "PUT",
      body: JSON.stringify(takeoffItem),
    }),
  
  delete: (id: string) =>
    apiFetch<void>(`/takeoff/${id}`, {
      method: "DELETE",
    }),
};

// === AUTH API ===
export const authApi = {
  login: async (username: string, password: string) => {
    const response = await apiFetch<{ token: string; user: any }>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    });
    
    if (response.success && response.data.token) {
      setAuthToken(response.data.token);
    }
    
    return response;
  },
  
  register: (username: string, email: string, password: string) =>
    apiFetch<{ token: string; user: any }>("/auth/register", {
      method: "POST",
      body: JSON.stringify({ username, email, password }),
    }),
  
  logout: () => {
    clearAuthToken();
  },
  
  getCurrentUser: () => apiFetch<any>("/auth/me"),
};

// === BLUEPRINTS API ===
export const blueprintsApi = {
  upload: async (projectId: string, file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("projectId", projectId);

    const token = getAuthToken();
    const headers: HeadersInit = {};
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE_URL}/blueprints/upload`, {
      method: "POST",
      headers,
      body: formData,
    });

    if (!response.ok) {
      throw new Error("Failed to upload blueprint");
    }

    return await response.json();
  },
  
  getByProjectId: (projectId: string) =>
    apiFetch<any[]>(`/blueprints/project/${projectId}`),
  
  delete: (id: string) =>
    apiFetch<void>(`/blueprints/${id}`, {
      method: "DELETE",
    }),
};
