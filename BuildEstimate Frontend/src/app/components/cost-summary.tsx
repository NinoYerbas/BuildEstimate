import { DollarSign, FileText, Download } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Button } from "./ui/button";
import { Separator } from "./ui/separator";
import type { TakeoffItem } from "./measurement-panel";

interface CostSummaryProps {
  items: TakeoffItem[];
}

export function CostSummary({ items }: CostSummaryProps) {
  const subtotal = items.reduce(
    (sum, item) => sum + item.quantity * item.unitPrice,
    0
  );
  const taxRate = 0.08; // 8% tax
  const tax = subtotal * taxRate;
  const total = subtotal + tax;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const itemsByType = items.reduce((acc, item) => {
    if (!acc[item.type]) {
      acc[item.type] = { count: 0, cost: 0 };
    }
    acc[item.type].count++;
    acc[item.type].cost += item.quantity * item.unitPrice;
    return acc;
  }, {} as Record<string, { count: number; cost: number }>);

  const handleExport = () => {
    const csv = [
      ["Item Name", "Type", "Quantity", "Unit", "Unit Price", "Total", "Notes"],
      ...items.map((item) => [
        item.name,
        item.type,
        item.quantity,
        item.unit,
        item.unitPrice,
        item.quantity * item.unitPrice,
        item.notes,
      ]),
      [],
      ["", "", "", "", "Subtotal", subtotal],
      ["", "", "", "", "Tax (8%)", tax],
      ["", "", "", "", "Total", total],
    ];

    const csvContent = csv.map((row) => row.join(",")).join("\n");
    const blob = new Blob([csvContent], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `takeoff-${new Date().toISOString().split("T")[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <DollarSign className="w-5 h-5" />
            Cost Summary
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-gray-600">Linear Items</span>
              <span className="font-medium">
                {itemsByType.linear?.count || 0} items -{" "}
                {formatCurrency(itemsByType.linear?.cost || 0)}
              </span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-600">Area Items</span>
              <span className="font-medium">
                {itemsByType.area?.count || 0} items -{" "}
                {formatCurrency(itemsByType.area?.cost || 0)}
              </span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-600">Count Items</span>
              <span className="font-medium">
                {itemsByType.count?.count || 0} items -{" "}
                {formatCurrency(itemsByType.count?.cost || 0)}
              </span>
            </div>
          </div>

          <Separator />

          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Subtotal</span>
              <span className="font-medium">{formatCurrency(subtotal)}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-600">Tax (8%)</span>
              <span>{formatCurrency(tax)}</span>
            </div>
            <Separator />
            <div className="flex justify-between text-lg">
              <span>Total</span>
              <span className="font-bold text-blue-600">
                {formatCurrency(total)}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Export
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Button onClick={handleExport} className="w-full" variant="outline">
            <Download className="w-4 h-4 mr-2" />
            Export to CSV
          </Button>
        </CardContent>
      </Card>

      <Card className="bg-blue-50 border-blue-200">
        <CardContent className="pt-6">
          <div className="text-sm text-gray-700 space-y-2">
            <p className="font-medium">Quick Stats:</p>
            <ul className="list-disc list-inside space-y-1 text-sm">
              <li>Total Items: {items.length}</li>
              <li>
                Avg Item Cost:{" "}
                {formatCurrency(items.length > 0 ? subtotal / items.length : 0)}
              </li>
              <li>
                Est. Completion: {items.length > 0 ? "Ready for review" : "Add items to start"}
              </li>
            </ul>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
