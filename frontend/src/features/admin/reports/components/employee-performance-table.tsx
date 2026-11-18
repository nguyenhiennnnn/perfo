import { Users } from "lucide-react";
import type { ReactNode } from "react";
import { Card, CardAction, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { UseQueryResult } from "@tanstack/react-query";
import { QueryState } from "@/components/common/query-state";
import { TableRowsSkeleton } from "@/components/common/skeleton-shapes";
import { EmptyState } from "@/components/common/empty-state";
import { formatCurrency, formatNumber } from "@/lib/formatters";
import type { EmployeePerformanceDto } from "../types";

interface EmployeePerformanceTableProps {
  query: UseQueryResult<EmployeePerformanceDto[]>;
  action?: ReactNode;
}

export const EmployeePerformanceTable = ({ query, action }: EmployeePerformanceTableProps) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Hiệu suất nhân viên</CardTitle>
        <CardDescription>Số vé, số đơn và doanh thu theo nhân viên</CardDescription>
        {action && <CardAction>{action}</CardAction>}
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={
            <Table>
              <TableBody>
                <TableRowsSkeleton rows={5} columns={4} />
              </TableBody>
            </Table>
          }
          isEmpty={(employees) => employees.length === 0}
          empty={<EmptyState icon={Users} title="Chưa có hoạt động nhân viên" description="Chưa có hoạt động bán vé nào trong khoảng thời gian này." />}
          errorTitle="Không tải được hiệu suất nhân viên"
        >
          {(employees) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Nhân viên</TableHead>
                <TableHead className="text-right">Vé bán</TableHead>
                <TableHead className="text-right">Số đơn</TableHead>
                <TableHead className="text-right">Doanh thu</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {employees.map((emp) => (
                <TableRow key={emp.employeeId}>
                  <TableCell className="font-medium text-foreground">{emp.fullName}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatNumber(emp.ticketsSold)}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatNumber(emp.bookingCount)}</TableCell>
                  <TableCell className="text-right tabular-nums font-medium text-primary-text">
                    {formatCurrency(emp.revenue)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
}

