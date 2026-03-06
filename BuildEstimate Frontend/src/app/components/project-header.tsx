import { FileText, Plus } from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { UserMenu } from "./user-menu";

interface ProjectHeaderProps {
  projectName: string;
  onProjectNameChange: (name: string) => void;
  onNewProject: () => void;
  onLogout?: () => void;
}

export function ProjectHeader({ projectName, onProjectNameChange, onNewProject, onLogout }: ProjectHeaderProps) {
  return (
    <div className="border-b bg-white p-4">
      <div className="max-w-7xl mx-auto flex items-center justify-between">
        <div className="flex items-center gap-4">
          <FileText className="w-8 h-8 text-blue-600" />
          <div className="flex items-center gap-2">
            <Input
              value={projectName}
              onChange={(e) => onProjectNameChange(e.target.value)}
              className="text-xl font-semibold border-0 shadow-none p-0 h-auto focus-visible:ring-0"
              placeholder="Project Name"
            />
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button onClick={onNewProject} variant="outline" size="sm">
            <Plus className="w-4 h-4 mr-2" />
            New Project
          </Button>
          {onLogout && <UserMenu onLogout={onLogout} />}
        </div>
      </div>
    </div>
  );
}