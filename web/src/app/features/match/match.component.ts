import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Demand, Match } from '../../core/models';

@Component({
  selector: 'app-match',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatProgressBarModule],
  template: `
    <a mat-button routerLink="/demands"><mat-icon>arrow_back</mat-icon> Demands</a>

    @if (demand(); as d) {
      <mat-card class="head">
        <h1>{{ d.title }}</h1>
        <p class="sub">{{ d.clientName }} · needs <b>{{ d.requiredSeniority }}</b> · status {{ d.status }}</p>
        <mat-chip-set>@for (s of d.requiredSkills; track s) { <mat-chip color="primary" highlighted>{{ s }}</mat-chip> }</mat-chip-set>
      </mat-card>
    }

    <h2>Recommended consultants</h2>
    <p class="explain">Ranked by a transparent score: availability, seniority fit and matched skills.</p>

    @if (busy()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    @for (m of matches(); track m.consultantId; let i = $index) {
      <mat-card class="match" [class.top]="i === 0">
        <div class="score" [class.good]="m.score >= 70" [class.mid]="m.score >= 40 && m.score < 70">{{ m.score }}</div>
        <div class="body">
          <div class="who">{{ m.name }} <span class="tag">{{ m.seniority }} · {{ m.availability }}</span></div>
          <div class="skills">
            @for (s of m.matchedSkills; track s) { <mat-chip class="ok" highlighted>{{ s }}</mat-chip> }
            @for (s of m.missingSkills; track s) { <mat-chip class="miss">{{ s }}</mat-chip> }
          </div>
          <div class="why">{{ m.explanation }}</div>
        </div>
        <button mat-flat-button color="primary" [disabled]="m.availability === 'Allocated' || busy()"
                (click)="allocate(m)">
          <mat-icon>person_add</mat-icon> Allocate
        </button>
      </mat-card>
    } @empty { @if (!busy()) { <p class="muted">No consultants to rank.</p> } }
  `,
  styles: [`
    .head { margin: 12px 0 20px; padding: 16px; }
    .head h1 { margin: 0 0 4px; } .sub { color: #666; margin: 0 0 10px; }
    h2 { margin: 8px 0 2px; } .explain { color: #777; margin: 0 0 14px; font-size: .88rem; }
    .match { display: flex; align-items: center; gap: 16px; padding: 14px 16px; margin-bottom: 10px; }
    .match.top { outline: 2px solid #1565c0; }
    .score { font-size: 1.7rem; font-weight: 700; width: 56px; text-align: center; color: #b71c1c; }
    .score.mid { color: #ef6c00; } .score.good { color: #2e7d32; }
    .body { flex: 1 1 auto; }
    .who { font-weight: 600; } .tag { color: #888; font-weight: 400; font-size: .85rem; }
    .skills { margin: 6px 0; display: flex; flex-wrap: wrap; gap: 4px; }
    mat-chip.ok { background: #e8f5e9 !important; } mat-chip.miss { background: #ffebee !important; opacity: .8; }
    .why { color: #555; font-size: .88rem; }
    .muted { color: #999; }
  `],
})
export class MatchComponent {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  demand = signal<Demand | null>(null);
  matches = signal<Match[]>([]);
  busy = signal(false);
  private id = Number(this.route.snapshot.paramMap.get('id'));

  constructor() {
    this.api.getDemand(this.id).subscribe(d => this.demand.set(d));
    this.load();
  }

  private load(): void {
    this.busy.set(true);
    this.api.matches(this.id).subscribe({
      next: m => { this.matches.set(m); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  allocate(m: Match): void {
    this.busy.set(true);
    const today = new Date().toISOString().slice(0, 10);
    this.api.allocate(this.id, m.consultantId, today).subscribe({
      next: () => {
        this.snack.open(`${m.name} allocated to this demand.`, 'OK', { duration: 3000 });
        this.router.navigate(['/allocations']);
      },
      error: () => { this.snack.open('Could not allocate.', 'OK', { duration: 3000 }); this.busy.set(false); },
    });
  }
}
