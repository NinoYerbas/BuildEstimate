import { useState, useEffect } from "react";
import { ProjectHeader } from "./components/project-header";
import { MeasurementPanel, type TakeoffItem } from "./components/measurement-panel";
import { TakeoffList } from "./components/takeoff-list";
import { CostSummary } from "./components/cost-summary";
import { BlueprintCanvas } from "./components/blueprint-canvas";
import { LoginForm } from "./components/login-form";
import { getAuthToken, takeoffApi, projectsApi, type Project } from "./services/api";
import { toast } from "sonner";
import { Toaster } from "./components/ui/sonner";

export default function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [currentProject, setCurrentProject] = useState<Project | null>(null);
  const [items, setItems] = useState<TakeoffItem[]>([]);

  // Check authentication on mount
  useEffect(() => {
    const token = getAuthToken();
    setIsAuthenticated(!!token);
    setIsLoading(false);
  }, []);

  // Load project and takeoff items when authenticated
  useEffect(() => {
    if (isAuthenticated && currentProject) {
      loadTakeoffItems(currentProject.id);
    }
  }, [isAuthenticated, currentProject]);

  const loadTakeoffItems = async (projectId: string) => {
    try {
      const response = await takeoffApi.getByProjectId(projectId);
      if (response.success) {
        setItems(response.data);
      }
    } catch (error) {
      toast.error("Failed to load takeoff items");
      console.error(error);
    }
  };

  const handleAddItem = async (newItem: Omit<TakeoffItem, "id">) => {
    if (!currentProject) {
      // Create temporary project if none exists
      await handleCreateDefaultProject();
      return;
    }

    try {
      const itemData = {
        ...newItem,
        projectId: currentProject.id,
      };
      
      const response = await takeoffApi.create(itemData);
      if (response.success) {
        setItems([...items, response.data]);
        toast.success("Item added successfully");
      }
    } catch (error) {
      toast.error("Failed to add item");
      console.error(error);
    }
  };

  const handleMeasurementComplete = async (measurement: {
    type: "linear" | "area";
    value: number;
  }) => {
    if (!currentProject) {
      await handleCreateDefaultProject();
      return;
    }

    const itemData = {
      name: `${measurement.type === "linear" ? "Line" : "Area"} Measurement`,
      type: measurement.type,
      quantity: measurement.value,
      unit: measurement.type === "linear" ? "ft" : "sq ft",
      unitPrice: 0,
      notes: "From blueprint",
      projectId: currentProject.id,
    };

    try {
      const response = await takeoffApi.create(itemData);
      if (response.success) {
        setItems([...items, response.data]);
        toast.success("Measurement added");
      }
    } catch (error) {
      toast.error("Failed to save measurement");
      console.error(error);
    }
  };

  const handleDeleteItem = async (id: string) => {
    try {
      await takeoffApi.delete(id);
      setItems(items.filter((item) => item.id !== id));
      toast.success("Item deleted");
    } catch (error) {
      toast.error("Failed to delete item");
      console.error(error);
    }
  };

  const handleCreateDefaultProject = async () => {
    try {
      const response = await projectsApi.create({
        name: "New Takeoff Project",
        isPrevailingWage: false,
      });
      if (response.success) {
        setCurrentProject(response.data);
        toast.success("Project created");
      }
    } catch (error) {
      toast.error("Failed to create project");
      console.error(error);
    }
  };

  const handleNewProject = async () => {
    if (items.length > 0) {
      const confirmed = window.confirm(
        "This will create a new project. Continue?"
      );
      if (!confirmed) return;
    }
    
    await handleCreateDefaultProject();
  };

  const handleProjectNameChange = async (name: string) => {
    if (!currentProject) return;
    
    try {
      const response = await projectsApi.update(currentProject.id, { name });
      if (response.success) {
        setCurrentProject(response.data);
      }
    } catch (error) {
      console.error("Failed to update project name", error);
    }
  };

  const handleLoginSuccess = async () => {
    setIsAuthenticated(true);
    toast.success("Logged in successfully");
    
    // Create default project on first login
    await handleCreateDefaultProject();
  };

  const handleLogout = () => {
    setIsAuthenticated(false);
    setCurrentProject(null);
    setItems([]);
    toast.success("Logged out successfully");
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <p>Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <>
        <LoginForm onLoginSuccess={handleLoginSuccess} />
        <Toaster />
      </>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <ProjectHeader
        projectName={currentProject?.name || "New Takeoff Project"}
        onProjectNameChange={handleProjectNameChange}
        onNewProject={handleNewProject}
        onLogout={handleLogout}
      />

      <div className="max-w-7xl mx-auto p-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <BlueprintCanvas onMeasurementComplete={handleMeasurementComplete} />
            <MeasurementPanel onAddItem={handleAddItem} />
            <TakeoffList items={items} onDeleteItem={handleDeleteItem} />
          </div>
          <div className="lg:col-span-1">
            <CostSummary items={items} />
          </div>
        </div>
      </div>
      <Toaster />
    </div>
  );
}