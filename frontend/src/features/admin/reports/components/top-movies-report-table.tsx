import { Trophy } from "lucide-react";
import type { ReactNode } from "react";
import { Card, CardAction, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { UseQueryResult } from "@tanstack/react-query";
import { QueryState } from "@/components/common/query-state";
import { TableRowsSkeleton } from "@/components/common/skeleton-shapes";
import { EmptyState } from "@/components/common/empty-state";
import { formatCurrency, formatNumber } from "@/lib/formatters";
import type { TopMovieDto } from "../types";

interface TopMoviesReportTableProps {
  query: UseQueryResult<TopMovieDto[]>;
  action?: ReactNode;
}

export const TopMoviesReportTable = ({ query, action }: TopMoviesReportTableProps) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Phim bán chạy nhất</CardTitle>
        <CardDescription>Xếp hạng theo số vé và doanh thu</CardDescription>
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
          isEmpty={(movies) => movies.length === 0}
          empty={<EmptyState icon={Trophy} title="Chưa có phim bán chạy" description="Chưa có phim nào bán vé trong khoảng này." />}
          errorTitle="Không tải được phim bán chạy"
        >
          {(movies) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">#</TableHead>
                <TableHead>Phim</TableHead>
                <TableHead className="text-right">Vé bán</TableHead>
                <TableHead className="text-right">Doanh thu</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {movies.map((movie, index) => (
                <TableRow key={movie.movieId}>
                  <TableCell className="text-muted-foreground">{index + 1}</TableCell>
                  <TableCell className="font-medium text-foreground">{movie.title}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatNumber(movie.ticketsSold)}</TableCell>
                  <TableCell className="text-right tabular-nums font-medium text-primary">
                    {formatCurrency(movie.revenue)}
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

