import { Trash2, Edit2, Ruler, Square, Hash } from "lucide-react";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "./ui/table";
import { Badge } from "./ui/badge";
import type { TakeoffItem } from "./measurement-panel";

interface TakeoffListProps {
  items: TakeoffItem[];
  onDeleteItem: (id: string) => void;
}

export function TakeoffList({ items, onDeleteItem }: TakeoffListProps) {
  const getTypeIcon = (type: TakeoffItem["type"]) => {
    switch (type) {
      case "linear":
        return <Ruler className="w-4 h-4" />;
      case "area":
        return <Square className="w-4 h-4" />;
      case "count":
        return <Hash className="w-4 h-4" />;
    }
  };

  const getTypeColor = (type: TakeoffItem["type"]) => {
    switch (type) {
      case "linear":
        return "bg-blue-500";
      case "area":
        return "bg-green-500";
      case "count":
        return "bg-purple-500";
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Takeoff Items ({items.length})</CardTitle>
      </CardHeader>
      <CardContent>
        {items.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <Ruler className="w-12 h-12 mx-auto mb-4 opacity-20" />
            <p>No items added yet</p>
            <p className="text-sm">Start by adding measurements above</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">Type</TableHead>
                  <TableHead>Item Name</TableHead>
                  <TableHead className="text-right">Quantity</TableHead>
                  <TableHead className="text-right">Unit Price</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                  <TableHead>Notes</TableHead>
                  <TableHead className="w-12"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <div
                        className={`w-8 h-8 rounded flex items-center justify-center text-white ${getTypeColor(
                          item.type
                        )}`}
                      >
                        {getTypeIcon(item.type)}
                      </div>
                    </TableCell>
                    <TableCell className="font-medium">{item.name}</TableCell>
                    <TableCell className="text-right">
                      {item.quantity.toLocaleString()} {item.unit}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(item.unitPrice)}
                    </TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrency(item.quantity * item.unitPrice)}
                    </TableCell>
                    <TableCell className="text-sm text-gray-600">
                      {item.notes || "-"}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => onDeleteItem(item.id)}
                      >
                        <Trash2 className="w-4 h-4 text-red-600" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
