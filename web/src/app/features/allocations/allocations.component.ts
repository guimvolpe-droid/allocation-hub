import { Component, inject, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Allocation } from '../../core/models';

@Component({
  selector: 'app-allocations',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatIconModule],
  template: `
    <h1>Allocations</h1>
    <table mat-table [dataSource]="rows()" class="mat-elevation-z1 full">
      <ng-container matColumnDef="consultant"><th mat-header-cell *matHeaderCellDef>Consultant</th><td mat-cell *matCellDef="let a"><b>{{ a.consultantName }}</b></td></ng-container>
      <ng-container matColumnDef="demand"><th mat-header-cell *matHeaderCellDef>Demand</th><td mat-cell *matCellDef="let a">{{ a.demandTitle }}</td></ng-container>
      <ng-container matColumnDef="start"><th mat-header-cell *matHeaderCellDef>Start</th><td mat-cell *matCellDef="let a">{{ a.startDate }}</td></ng-container>
      <ng-container matColumnDef="end"><th mat-header-cell *matHeaderCellDef>End</th><td mat-cell *matCellDef="let a">{{ a.endDate ?? '—' }}</td></ng-container>
      <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let a"><span class="pill" [class]="a.status.toLowerCase()">{{ a.status }}</span></td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let a" class="actions">
          @if (a.status === 'Active') { <button mat-stroked-button (click)="end(a)"><mat-icon>stop_circle</mat-icon> End</button> }
        </td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
  `,
  styles: [`
    h1 { margin: 0 0 16px; } .full { width: 100%; } .actions { text-align: right; }
    .pill { padding: 2px 8px; border-radius: 10px; font-size: .78rem; }
    .pill.active { background: #e8f5e9; color: #2e7d32; } .pill.ended { background: #f0f0f0; color: #777; }
  `],
})
export class AllocationsComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  cols = ['consultant', 'demand', 'start', 'end', 'status', 'actions'];
  rows = signal<Allocation[]>([]);

  constructor() { this.load(); }
  private load() { this.api.listAllocations().subscribe(r => this.rows.set(r)); }

  end(a: Allocation) {
    if (!confirm(`End ${a.consultantName}'s allocation? This frees them and re-opens the demand.`)) return;
    this.api.endAllocation(a.id).subscribe(() => { this.snack.open('Allocation ended.', 'OK', { duration: 2000 }); this.load(); });
  }
}
