import { Ruler, Square, Hash, Plus } from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";

export type MeasurementType = "linear" | "area" | "count";

export interface TakeoffItem {
  id: string;
  name: string;
  type: MeasurementType;
  quantity: number;
  unit: string;
  unitPrice: number;
  notes: string;
}

interface MeasurementPanelProps {
  onAddItem: (item: Omit<TakeoffItem, "id">) => void;
}

export function MeasurementPanel({ onAddItem }: MeasurementPanelProps) {
  const [name, setName] = React.useState("");
  const [type, setType] = React.useState<MeasurementType>("linear");
  const [quantity, setQuantity] = React.useState("");
  const [unit, setUnit] = React.useState("ft");
  const [unitPrice, setUnitPrice] = React.useState("");
  const [notes, setNotes] = React.useState("");

  const handleAddItem = () => {
    if (!name || !quantity) return;

    onAddItem({
      name,
      type,
      quantity: parseFloat(quantity),
      unit,
      unitPrice: parseFloat(unitPrice) || 0,
      notes,
    });

    // Reset form
    setName("");
    setQuantity("");
    setUnitPrice("");
    setNotes("");
  };

  const getUnitOptions = () => {
    switch (type) {
      case "linear":
        return ["ft", "in", "m", "yd"];
      case "area":
        return ["sq ft", "sq m", "sq yd", "acre"];
      case "count":
        return ["ea", "pcs", "units"];
      default:
        return ["ft"];
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Plus className="w-5 h-5" />
          Add Measurement
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-3 gap-2">
          <Button
            variant={type === "linear" ? "default" : "outline"}
            onClick={() => {
              setType("linear");
              setUnit("ft");
            }}
            className="flex flex-col h-auto py-3"
          >
            <Ruler className="w-5 h-5 mb-1" />
            <span className="text-xs">Linear</span>
          </Button>
          <Button
            variant={type === "area" ? "default" : "outline"}
            onClick={() => {
              setType("area");
              setUnit("sq ft");
            }}
            className="flex flex-col h-auto py-3"
          >
            <Square className="w-5 h-5 mb-1" />
            <span className="text-xs">Area</span>
          </Button>
          <Button
            variant={type === "count" ? "default" : "outline"}
            onClick={() => {
              setType("count");
              setUnit("ea");
            }}
            className="flex flex-col h-auto py-3"
          >
            <Hash className="w-5 h-5 mb-1" />
            <span className="text-xs">Count</span>
          </Button>
        </div>

        <div className="space-y-2">
          <Label htmlFor="item-name">Item Name</Label>
          <Input
            id="item-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g., 2x4 Lumber, Drywall, Outlets"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="quantity">Quantity</Label>
            <Input
              id="quantity"
              type="number"
              step="0.01"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              placeholder="0.00"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="unit">Unit</Label>
            <Select value={unit} onValueChange={setUnit}>
              <SelectTrigger id="unit">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {getUnitOptions().map((u) => (
                  <SelectItem key={u} value={u}>
                    {u}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="unit-price">Unit Price ($)</Label>
          <Input
            id="unit-price"
            type="number"
            step="0.01"
            value={unitPrice}
            onChange={(e) => setUnitPrice(e.target.value)}
            placeholder="0.00"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="notes">Notes (Optional)</Label>
          <Input
            id="notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Additional details"
          />
        </div>

        <Button onClick={handleAddItem} className="w-full">
          <Plus className="w-4 h-4 mr-2" />
          Add Item
        </Button>
      </CardContent>
    </Card>
  );
}

import React from "react";
