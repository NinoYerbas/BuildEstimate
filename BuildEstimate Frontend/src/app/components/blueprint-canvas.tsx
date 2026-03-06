import { useRef, useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import {
  Ruler,
  Square,
  Trash2,
  Upload,
  ZoomIn,
  ZoomOut,
  Move,
} from "lucide-react";
import { Badge } from "./ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";

type Tool = "pan" | "line" | "rectangle" | "select";

interface Measurement {
  id: string;
  type: "line" | "rectangle";
  points: number[];
  color: string;
  length?: number;
  area?: number;
}

interface BlueprintCanvasProps {
  onMeasurementComplete: (measurement: {
    type: "linear" | "area";
    value: number;
  }) => void;
}

// Architectural scale presets
const IMPERIAL_SCALES = [
  { label: '1/16" = 1\'-0"', value: 1 / 16, description: "Large site plans" },
  { label: '1/8" = 1\'-0"', value: 1 / 8, description: "Commercial blueprints" },
  { label: '3/16" = 1\'-0"', value: 3 / 16, description: "Floor plans" },
  { label: '1/4" = 1\'-0"', value: 1 / 4, description: "Residential plans (most common)" },
  { label: '3/8" = 1\'-0"', value: 3 / 8, description: "Detailed floor plans" },
  { label: '1/2" = 1\'-0"', value: 1 / 2, description: "Detail drawings" },
  { label: '3/4" = 1\'-0"', value: 3 / 4, description: "Large scale details" },
  { label: '1" = 1\'-0"', value: 1, description: "Full size details" },
  { label: '1 1/2" = 1\'-0"', value: 1.5, description: "Connection details" },
  { label: '3" = 1\'-0"', value: 3, description: "Intricate construction details" },
];

const METRIC_SCALES = [
  { label: "1:500", value: 500, description: "Site plans" },
  { label: "1:200", value: 200, description: "Site layouts" },
  { label: "1:100", value: 100, description: "Floor plans" },
  { label: "1:50", value: 50, description: "Detailed plans (common)" },
  { label: "1:25", value: 25, description: "Component details" },
  { label: "1:20", value: 20, description: "Construction details" },
  { label: "1:10", value: 10, description: "Large scale details" },
  { label: "1:5", value: 5, description: "Intricate details" },
];

export function BlueprintCanvas({ onMeasurementComplete }: BlueprintCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [image, setImage] = useState<HTMLImageElement | null>(null);
  const [tool, setTool] = useState<Tool>("pan");
  const [measurements, setMeasurements] = useState<Measurement[]>([]);
  const [currentPoints, setCurrentPoints] = useState<number[]>([]);
  const [isDrawing, setIsDrawing] = useState(false);
  const [scale, setScale] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [isPanning, setIsPanning] = useState(false);
  const [lastPanPoint, setLastPanPoint] = useState({ x: 0, y: 0 });
  const [pixelsPerFoot, setPixelsPerFoot] = useState<number>(100);
  const [calibrationMode, setCalibrationMode] = useState(false);
  const [calibrationPoints, setCalibrationPoints] = useState<number[]>([]);
  const [knownDistance, setKnownDistance] = useState("10");
  const [scaleSystem, setScaleSystem] = useState<"imperial" | "metric">("imperial");
  const [selectedScale, setSelectedScale] = useState<string>("");
  const [currentScaleLabel, setCurrentScaleLabel] = useState<string>("Custom");

  useEffect(() => {
    redraw();
  }, [image, measurements, currentPoints, scale, offset]);

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const img = new Image();
      img.onload = () => {
        setImage(img);
        // Reset view
        setScale(1);
        setOffset({ x: 0, y: 0 });
        setMeasurements([]);
      };
      img.src = event.target?.result as string;
    };
    reader.readAsDataURL(file);
  };

  const getMousePos = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };

    const rect = canvas.getBoundingClientRect();
    return {
      x: (e.clientX - rect.left - offset.x) / scale,
      y: (e.clientY - rect.top - offset.y) / scale,
    };
  };

  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const pos = getMousePos(e);

    if (calibrationMode) {
      if (calibrationPoints.length < 4) {
        setCalibrationPoints([...calibrationPoints, pos.x, pos.y]);
      }
      return;
    }

    if (tool === "pan") {
      setIsPanning(true);
      setLastPanPoint({ x: e.clientX, y: e.clientY });
    } else if (tool === "line") {
      setIsDrawing(true);
      setCurrentPoints([pos.x, pos.y]);
    } else if (tool === "rectangle") {
      setIsDrawing(true);
      setCurrentPoints([pos.x, pos.y]);
    }
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const pos = getMousePos(e);

    if (isPanning) {
      const dx = e.clientX - lastPanPoint.x;
      const dy = e.clientY - lastPanPoint.y;
      setOffset({ x: offset.x + dx, y: offset.y + dy });
      setLastPanPoint({ x: e.clientX, y: e.clientY });
    } else if (isDrawing && currentPoints.length >= 2) {
      setCurrentPoints([currentPoints[0], currentPoints[1], pos.x, pos.y]);
    }
  };

  const handleMouseUp = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (isPanning) {
      setIsPanning(false);
    } else if (isDrawing && currentPoints.length === 4) {
      const [x1, y1, x2, y2] = currentPoints;

      if (tool === "line") {
        const distance = Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
        const realDistance = distance / pixelsPerFoot;

        const measurement: Measurement = {
          id: crypto.randomUUID(),
          type: "line",
          points: currentPoints,
          color: "#3b82f6",
          length: realDistance,
        };
        setMeasurements([...measurements, measurement]);
        onMeasurementComplete({ type: "linear", value: realDistance });
      } else if (tool === "rectangle") {
        const width = Math.abs(x2 - x1);
        const height = Math.abs(y2 - y1);
        const area = (width * height) / (pixelsPerFoot * pixelsPerFoot);

        const measurement: Measurement = {
          id: crypto.randomUUID(),
          type: "rectangle",
          points: currentPoints,
          color: "#10b981",
          area: area,
        };
        setMeasurements([...measurements, measurement]);
        onMeasurementComplete({ type: "area", value: area });
      }

      setCurrentPoints([]);
      setIsDrawing(false);
    }
  };

  const redraw = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = "#f3f4f6";
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.save();
    ctx.translate(offset.x, offset.y);
    ctx.scale(scale, scale);

    // Draw image
    if (image) {
      ctx.drawImage(image, 0, 0);
    }

    // Draw measurements
    measurements.forEach((m) => {
      ctx.strokeStyle = m.color;
      ctx.lineWidth = 2 / scale;

      if (m.type === "line") {
        const [x1, y1, x2, y2] = m.points;
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();

        // Draw circles at endpoints
        ctx.fillStyle = m.color;
        ctx.beginPath();
        ctx.arc(x1, y1, 4 / scale, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(x2, y2, 4 / scale, 0, Math.PI * 2);
        ctx.fill();

        // Draw label
        if (m.length) {
          const midX = (x1 + x2) / 2;
          const midY = (y1 + y2) / 2;
          ctx.fillStyle = "#fff";
          ctx.fillRect(midX - 20, midY - 10, 40, 20);
          ctx.fillStyle = "#000";
          ctx.font = `${12 / scale}px sans-serif`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          ctx.fillText(`${m.length.toFixed(1)} ft`, midX, midY);
        }
      } else if (m.type === "rectangle") {
        const [x1, y1, x2, y2] = m.points;
        ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);

        // Draw label
        if (m.area) {
          const centerX = (x1 + x2) / 2;
          const centerY = (y1 + y2) / 2;
          ctx.fillStyle = "#fff";
          ctx.fillRect(centerX - 30, centerY - 10, 60, 20);
          ctx.fillStyle = "#000";
          ctx.font = `${12 / scale}px sans-serif`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          ctx.fillText(`${m.area.toFixed(1)} sq ft`, centerX, centerY);
        }
      }
    });

    // Draw current drawing
    if (currentPoints.length === 4) {
      ctx.strokeStyle = tool === "line" ? "#3b82f6" : "#10b981";
      ctx.lineWidth = 2 / scale;
      ctx.setLineDash([5 / scale, 5 / scale]);

      const [x1, y1, x2, y2] = currentPoints;

      if (tool === "line") {
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
      } else if (tool === "rectangle") {
        ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
      }

      ctx.setLineDash([]);
    }

    // Draw calibration
    if (calibrationMode && calibrationPoints.length >= 2) {
      ctx.strokeStyle = "#ef4444";
      ctx.lineWidth = 3 / scale;
      ctx.beginPath();
      ctx.moveTo(calibrationPoints[0], calibrationPoints[1]);
      if (calibrationPoints.length >= 4) {
        ctx.lineTo(calibrationPoints[2], calibrationPoints[3]);
      }
      ctx.stroke();

      // Draw points
      for (let i = 0; i < calibrationPoints.length; i += 2) {
        ctx.fillStyle = "#ef4444";
        ctx.beginPath();
        ctx.arc(calibrationPoints[i], calibrationPoints[i + 1], 5 / scale, 0, Math.PI * 2);
        ctx.fill();
      }
    }

    ctx.restore();
  };

  const handleZoom = (delta: number) => {
    setScale(Math.max(0.1, Math.min(5, scale + delta)));
  };

  const handleCalibrate = () => {
    if (calibrationPoints.length === 4) {
      const [x1, y1, x2, y2] = calibrationPoints;
      const pixelDistance = Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
      const known = parseFloat(knownDistance);
      if (known > 0) {
        setPixelsPerFoot(pixelDistance / known);
        setCurrentScaleLabel("Custom");
      }
    }
    setCalibrationMode(false);
    setCalibrationPoints([]);
  };

  const handlePresetScale = (scaleValue: string) => {
    setSelectedScale(scaleValue);
    
    if (!calibrationPoints || calibrationPoints.length !== 4) {
      alert("Please mark a reference dimension on the drawing first by clicking 'Calibrate' and marking two points.");
      return;
    }

    const [x1, y1, x2, y2] = calibrationPoints;
    const pixelDistance = Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));

    if (scaleSystem === "imperial") {
      // For imperial: scaleValue is inches per foot on paper
      // If we measure something on screen and know the scale, we can calculate pixels per foot
      const scale = IMPERIAL_SCALES.find(s => s.value.toString() === scaleValue);
      if (scale) {
        // Assuming the marked distance is in real feet
        const known = parseFloat(knownDistance);
        if (known > 0) {
          setPixelsPerFoot(pixelDistance / known);
          setCurrentScaleLabel(scale.label);
        }
      }
    } else {
      // For metric: scaleValue is the ratio (e.g., 50 means 1:50)
      const ratio = parseFloat(scaleValue);
      const known = parseFloat(knownDistance);
      if (known > 0 && ratio > 0) {
        // Convert known distance to feet and calculate
        setPixelsPerFoot(pixelDistance / known);
        const scale = METRIC_SCALES.find(s => s.value.toString() === scaleValue);
        if (scale) {
          setCurrentScaleLabel(scale.label);
        }
      }
    }
    
    setCalibrationMode(false);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>Blueprint Viewer</span>
          <div className="flex gap-2 items-center">
            <Badge variant="outline" className="text-xs">
              {currentScaleLabel}
            </Badge>
            <Button
              size="sm"
              variant="outline"
              onClick={() => fileInputRef.current?.click()}
            >
              <Upload className="w-4 h-4 mr-2" />
              Upload
            </Button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*,.pdf"
          onChange={handleFileUpload}
          className="hidden"
        />

        {/* Tools */}
        <div className="flex gap-2 flex-wrap">
          <Button
            size="sm"
            variant={tool === "pan" ? "default" : "outline"}
            onClick={() => setTool("pan")}
          >
            <Move className="w-4 h-4 mr-2" />
            Pan
          </Button>
          <Button
            size="sm"
            variant={tool === "line" ? "default" : "outline"}
            onClick={() => setTool("line")}
          >
            <Ruler className="w-4 h-4 mr-2" />
            Measure Line
          </Button>
          <Button
            size="sm"
            variant={tool === "rectangle" ? "default" : "outline"}
            onClick={() => setTool("rectangle")}
          >
            <Square className="w-4 h-4 mr-2" />
            Measure Area
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleZoom(0.2)}
          >
            <ZoomIn className="w-4 h-4" />
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleZoom(-0.2)}
          >
            <ZoomOut className="w-4 h-4" />
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setMeasurements([])}
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Clear All
          </Button>
        </div>

        {/* Scale Calibration */}
        <div className="border rounded-lg p-4 bg-blue-50 space-y-3">
          <div className="flex items-center justify-between">
            <Label className="font-semibold">Drawing Scale</Label>
            <Badge variant="secondary">{scaleSystem === "imperial" ? "Imperial" : "Metric"}</Badge>
          </div>

          <Tabs value={scaleSystem} onValueChange={(v) => setScaleSystem(v as "imperial" | "metric")} className="w-full">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="imperial">Imperial (US)</TabsTrigger>
              <TabsTrigger value="metric">Metric</TabsTrigger>
            </TabsList>
            
            <TabsContent value="imperial" className="space-y-3 mt-3">
              <div className="grid grid-cols-2 gap-2">
                <div className="space-y-1">
                  <Label className="text-xs">Reference Length (ft)</Label>
                  <Input
                    type="number"
                    value={knownDistance}
                    onChange={(e) => setKnownDistance(e.target.value)}
                    placeholder="10"
                    className="h-8"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Drawing Scale</Label>
                  <Select value={selectedScale} onValueChange={handlePresetScale}>
                    <SelectTrigger className="h-8">
                      <SelectValue placeholder="Select scale" />
                    </SelectTrigger>
                    <SelectContent>
                      {IMPERIAL_SCALES.map((scale) => (
                        <SelectItem key={scale.value} value={scale.value.toString()}>
                          <div className="flex flex-col">
                            <span>{scale.label}</span>
                            <span className="text-xs text-gray-500">{scale.description}</span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant={calibrationMode ? "default" : "outline"}
                  onClick={() => {
                    if (calibrationMode && calibrationPoints.length === 4) {
                      handleCalibrate();
                    } else {
                      setCalibrationMode(!calibrationMode);
                      setCalibrationPoints([]);
                    }
                  }}
                  className="flex-1"
                >
                  {calibrationMode
                    ? calibrationPoints.length === 4
                      ? "Apply Custom"
                      : `Mark ${calibrationPoints.length === 0 ? "2" : "1"} point${calibrationPoints.length === 0 ? "s" : ""}`
                    : "Calibrate"}
                </Button>
              </div>
            </TabsContent>
            
            <TabsContent value="metric" className="space-y-3 mt-3">
              <div className="grid grid-cols-2 gap-2">
                <div className="space-y-1">
                  <Label className="text-xs">Reference Length (m)</Label>
                  <Input
                    type="number"
                    value={knownDistance}
                    onChange={(e) => setKnownDistance(e.target.value)}
                    placeholder="3"
                    className="h-8"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Drawing Scale</Label>
                  <Select value={selectedScale} onValueChange={handlePresetScale}>
                    <SelectTrigger className="h-8">
                      <SelectValue placeholder="Select scale" />
                    </SelectTrigger>
                    <SelectContent>
                      {METRIC_SCALES.map((scale) => (
                        <SelectItem key={scale.value} value={scale.value.toString()}>
                          <div className="flex flex-col">
                            <span>{scale.label}</span>
                            <span className="text-xs text-gray-500">{scale.description}</span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant={calibrationMode ? "default" : "outline"}
                  onClick={() => {
                    if (calibrationMode && calibrationPoints.length === 4) {
                      handleCalibrate();
                    } else {
                      setCalibrationMode(!calibrationMode);
                      setCalibrationPoints([]);
                    }
                  }}
                  className="flex-1"
                >
                  {calibrationMode
                    ? calibrationPoints.length === 4
                      ? "Apply Custom"
                      : `Mark ${calibrationPoints.length === 0 ? "2" : "1"} point${calibrationPoints.length === 0 ? "s" : ""}`
                    : "Calibrate"}
                </Button>
              </div>
            </TabsContent>
          </Tabs>
          
          <div className="text-xs text-gray-600 bg-white p-2 rounded border">
            <strong>How to calibrate:</strong>
            <ol className="list-decimal list-inside mt-1 space-y-0.5">
              <li>Find a dimension on your drawing (e.g., a 10' wall)</li>
              <li>Enter that dimension in "Reference Length"</li>
              <li>Click "Calibrate" and mark both ends of that dimension</li>
              <li>Select your drawing's scale from the dropdown OR click "Apply Custom"</li>
            </ol>
          </div>
        </div>

        {/* Canvas */}
        <div className="border rounded-lg overflow-hidden bg-gray-100">
          <canvas
            ref={canvasRef}
            width={800}
            height={600}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            className="cursor-crosshair w-full"
          />
        </div>

        {!image && (
          <div className="text-center py-12 text-gray-500">
            <Upload className="w-12 h-12 mx-auto mb-4 opacity-20" />
            <p>Upload a blueprint to start measuring</p>
            <p className="text-sm">Supports JPG, PNG, and PDF files</p>
          </div>
        )}

        {image && (
          <div className="text-sm text-gray-600 space-y-1">
            <p><strong>Quick Start:</strong></p>
            <ol className="list-decimal list-inside space-y-1">
              <li>Calibrate the scale using a known dimension</li>
              <li>Select a measurement tool (Line or Area)</li>
              <li>Click to place start and end points on the blueprint</li>
              <li>Measurements are automatically added to your takeoff list</li>
            </ol>
          </div>
        )}
      </CardContent>
    </Card>
  );
}