import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Demand, ExternalMatch, Match } from '../../core/models';

@Component({
  selector: 'app-match',
  standalone: true,
  imports: [
    RouterLink, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressBarModule, MatFormFieldModule, MatInputModule
  ],
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

    <!-- ── The "spice": rank REAL developers sourced live from the GitHub API ── -->
    <div class="ext-head">
      <h2>Source real candidates <span class="src">· GitHub</span></h2>
      <p class="explain">
        LinkedIn has no compliant people-search API — for a developer-staffing tool the real, official
        source is GitHub. Same matching rule, real public profiles.
      </p>
    </div>

    <div class="ext-controls">
      <mat-form-field appearance="outline" class="loc">
        <mat-label>Location (optional)</mat-label>
        <input matInput [(ngModel)]="location" placeholder="e.g. Brazil" (keyup.enter)="searchGitHub()">
      </mat-form-field>
      <button mat-flat-button color="accent" [disabled]="extBusy()" (click)="searchGitHub()">
        <mat-icon>travel_explore</mat-icon> Search GitHub
      </button>
    </div>

    @if (extBusy()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }
    @if (extError(); as e) { <p class="err">{{ e }}</p> }

    @for (m of externalMatches(); track m.externalId; let i = $index) {
      <mat-card class="match ext" [class.top]="i === 0">
        <div class="score" [class.good]="m.score >= 70" [class.mid]="m.score >= 40 && m.score < 70">{{ m.score }}</div>
        @if (m.avatarUrl) { <img class="avatar" [src]="m.avatarUrl" alt=""> }
        <div class="body">
          <div class="who">
            <a [href]="m.profileUrl" target="_blank" rel="noopener">{{ m.name }}</a>
            <span class="tag">{{ m.seniority }} · {{ m.source }}</span>
          </div>
          @if (m.headline) { <div class="headline">{{ m.headline }}</div> }
          <div class="skills">
            @for (s of m.matchedSkills; track s) { <mat-chip class="ok" highlighted>{{ s }}</mat-chip> }
            @for (s of m.missingSkills; track s) { <mat-chip class="miss">{{ s }}</mat-chip> }
          </div>
          <div class="why">{{ m.explanation }}</div>
        </div>
        <a mat-stroked-button [href]="m.profileUrl" target="_blank" rel="noopener">
          <mat-icon>open_in_new</mat-icon> GitHub
        </a>
      </mat-card>
    } @empty { @if (extSearched() && !extBusy() && !extError()) { <p class="muted">No GitHub candidates found for these skills.</p> } }
  `,
  styles: [`
    .head { margin: 12px 0 20px; padding: 16px; }
    .head h1 { margin: 0 0 4px; } .sub { color: #666; margin: 0 0 10px; }
    h2 { margin: 8px 0 2px; } .explain { color: #777; margin: 0 0 14px; font-size: .88rem; max-width: 620px; }
    .match { display: flex; align-items: center; gap: 16px; padding: 14px 16px; margin-bottom: 10px; }
    .match.top { outline: 2px solid #1565c0; }
    .score { font-size: 1.7rem; font-weight: 700; width: 56px; text-align: center; color: #b71c1c; }
    .score.mid { color: #ef6c00; } .score.good { color: #2e7d32; }
    .avatar { width: 44px; height: 44px; border-radius: 50%; }
    .body { flex: 1 1 auto; }
    .who { font-weight: 600; } .who a { color: #1565c0; text-decoration: none; } .who a:hover { text-decoration: underline; }
    .tag { color: #888; font-weight: 400; font-size: .85rem; }
    .headline { color: #666; font-size: .85rem; margin: 2px 0; }
    .skills { margin: 6px 0; display: flex; flex-wrap: wrap; gap: 4px; }
    mat-chip.ok { background: #e8f5e9 !important; } mat-chip.miss { background: #ffebee !important; opacity: .8; }
    .why { color: #555; font-size: .88rem; }
    .muted { color: #999; }
    .err { color: #b71c1c; font-size: .9rem; margin: 6px 0; }
    .ext-head { margin-top: 30px; border-top: 1px solid #eee; padding-top: 14px; }
    .ext-head .src { color: #6a1b9a; font-weight: 600; }
    .ext-controls { display: flex; gap: 12px; align-items: center; margin-bottom: 12px; }
    .ext-controls .loc { width: 220px; }
    .match.ext { background: #faf7fd; }
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

  // External (GitHub) sourcing state
  location = '';
  externalMatches = signal<ExternalMatch[]>([]);
  extBusy = signal(false);
  extError = signal<string | null>(null);
  extSearched = signal(false);

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

  searchGitHub(): void {
    this.extBusy.set(true);
    this.extError.set(null);
    this.extSearched.set(true);
    this.api.externalMatches(this.id, 'github', this.location.trim() || undefined).subscribe({
      next: m => { this.externalMatches.set(m); this.extBusy.set(false); },
      error: err => {
        this.externalMatches.set([]);
        this.extError.set(err?.error?.message ?? 'Could not reach the GitHub source.');
        this.extBusy.set(false);
      },
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
