import { AfterViewInit, Component, ViewChild, inject } from '@angular/core';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Allocation } from '../../core/models';

@Component({
  selector: 'app-allocations',
  standalone: true,
  imports: [
    MatTableModule, MatPaginatorModule, MatSortModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule,
  ],
  template: `
    <h1>Allocations</h1>
    <mat-form-field appearance="outline" class="search">
      <mat-icon matPrefix>search</mat-icon>
      <mat-label>Search consultant or demand…</mat-label>
      <input matInput (keyup)="applyFilter($event)" #f>
    </mat-form-field>
    <table mat-table [dataSource]="ds" matSort class="mat-elevation-z1 full">
      <ng-container matColumnDef="consultant"><th mat-header-cell *matHeaderCellDef mat-sort-header>Consultant</th><td mat-cell *matCellDef="let a"><b>{{ a.consultantName }}</b></td></ng-container>
      <ng-container matColumnDef="demand"><th mat-header-cell *matHeaderCellDef mat-sort-header>Demand</th><td mat-cell *matCellDef="let a">{{ a.demandTitle }}</td></ng-container>
      <ng-container matColumnDef="start"><th mat-header-cell *matHeaderCellDef mat-sort-header>Start</th><td mat-cell *matCellDef="let a">{{ a.startDate }}</td></ng-container>
      <ng-container matColumnDef="end"><th mat-header-cell *matHeaderCellDef>End</th><td mat-cell *matCellDef="let a">{{ a.endDate ?? '—' }}</td></ng-container>
      <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
        <td mat-cell *matCellDef="let a"><span class="pill" [class]="a.status.toLowerCase()">{{ a.status }}</span></td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let a" class="actions">
          @if (a.status === 'Active') { <button mat-stroked-button (click)="end(a)"><mat-icon>stop_circle</mat-icon> End</button> }
        </td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
      <tr class="empty" *matNoDataRow><td [attr.colspan]="cols.length">No allocations match “{{ f.value }}”.</td></tr>
    </table>
    <mat-paginator [pageSizeOptions]="[5, 10, 25]" pageSize="10" showFirstLastButtons></mat-paginator>
  `,
  styles: [`
    h1 { margin: 0 0 12px; } .full { width: 100%; } .actions { text-align: right; }
    .search { width: 100%; max-width: 420px; margin-bottom: 8px; }
    .pill { padding: 2px 8px; border-radius: 10px; font-size: .78rem; }
    .pill.active { background: #e8f5e9; color: #2e7d32; } .pill.ended { background: #f0f0f0; color: #777; }
    .empty td { padding: 20px; color: #999; text-align: center; }
  `],
})
export class AllocationsComponent implements AfterViewInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  cols = ['consultant', 'demand', 'start', 'end', 'status', 'actions'];
  ds = new MatTableDataSource<Allocation>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
    this.ds.filterPredicate = (a, f) =>
      `${a.consultantName} ${a.demandTitle} ${a.status}`.toLowerCase().includes(f);
    this.load();
  }

  ngAfterViewInit() { this.ds.paginator = this.paginator; this.ds.sort = this.sort; }

  applyFilter(e: Event) {
    this.ds.filter = (e.target as HTMLInputElement).value.trim().toLowerCase();
    this.ds.paginator?.firstPage();
  }

  private load() { this.api.listAllocations().subscribe(r => (this.ds.data = r)); }

  end(a: Allocation) {
    if (!confirm(`End ${a.consultantName}'s allocation? This frees them and re-opens the demand.`)) return;
    this.api.endAllocation(a.id).subscribe(() => { this.snack.open('Allocation ended.', 'OK', { duration: 2000 }); this.load(); });
  }
}
