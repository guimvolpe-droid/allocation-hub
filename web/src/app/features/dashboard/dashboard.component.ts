import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ApiService } from '../../core/api.service';
import { DashboardSummary } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatChipsModule, MatProgressBarModule],
  template: `
    <h1>Dashboard</h1>
    @if (data(); as d) {
      <div class="stats">
        <mat-card class="stat"><div class="n">{{ d.totalConsultants }}</div><div class="l">Consultants</div></mat-card>
        <mat-card class="stat ok"><div class="n">{{ d.availableConsultants }}</div><div class="l">Available</div></mat-card>
        <mat-card class="stat busy"><div class="n">{{ d.allocatedConsultants }}</div><div class="l">Allocated</div></mat-card>
        <mat-card class="stat open"><div class="n">{{ d.openDemands }}</div><div class="l">Open demands</div></mat-card>
      </div>

      <div class="cols">
        <mat-card class="panel">
          <h2>Top open demands</h2>
          @for (dem of d.topOpenDemands; track dem.id) {
            <div class="row">
              <div>
                <div class="title">{{ dem.title }}</div>
                <div class="sub">{{ dem.clientName }} · needs {{ dem.requiredSeniority }} · {{ dem.requiredSkills.join(', ') }}</div>
              </div>
              <a mat-stroked-button color="primary" [routerLink]="['/demands', dem.id, 'matches']">
                <mat-icon>auto_awesome</mat-icon> Matches
              </a>
            </div>
          } @empty { <p class="muted">No open demands.</p> }
        </mat-card>

        <mat-card class="panel">
          <h2>Available now</h2>
          @for (c of d.availableNow; track c.id) {
            <div class="row">
              <div>
                <div class="title">{{ c.name }}</div>
                <div class="sub">{{ c.seniority }} · {{ c.location }}</div>
              </div>
              <mat-chip-set>@for (s of c.skills.slice(0,3); track s) { <mat-chip>{{ s }}</mat-chip> }</mat-chip-set>
            </div>
          } @empty { <p class="muted">Nobody available.</p> }
        </mat-card>
      </div>
    } @else {
      @if (error()) { <p class="err">API unavailable — is the backend running on :5080?</p> }
      @else {
        <mat-progress-bar mode="indeterminate"></mat-progress-bar>
        <p class="muted">Loading dashboard…</p>
      }
    }
  `,
  styles: [`
    h1 { margin: 0 0 16px; }
    h2 { margin: 0 0 12px; font-size: 1.05rem; }
    .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 20px; }
    .stat { text-align: center; padding: 18px 8px; }
    .stat .n { font-size: 2.2rem; font-weight: 700; line-height: 1; }
    .stat .l { color: #666; font-size: .85rem; margin-top: 4px; }
    .stat.ok .n { color: var(--lv-ok); } .stat.busy .n { color: var(--lv-warn); } .stat.open .n { color: var(--lv-primary); }
    .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .panel { padding: 16px; }
    .row { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 0; border-top: 1px solid #eee; }
    .row:first-of-type { border-top: none; }
    .title { font-weight: 600; } .sub { color: #777; font-size: .82rem; }
    .muted { color: #999; }
    .err { color: var(--lv-err); }
    @media (max-width: 900px) { .stats { grid-template-columns: repeat(2,1fr); } .cols { grid-template-columns: 1fr; } }
  `],
})
export class DashboardComponent {
  private api = inject(ApiService);
  data = signal<DashboardSummary | null>(null);
  error = signal(false);

  constructor() {
    this.api.dashboard().subscribe({
      next: d => this.data.set(d),
      error: () => this.error.set(true), // fail loud, not a frozen "Loading…"
    });
  }
}
