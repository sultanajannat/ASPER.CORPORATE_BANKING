<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">

<section class="pe-console">
  <div class="container-fluid px-0">

    <!-- Page header -->
    <div class="pe-page-header px-2">
      <h4 class="pe-page-title">
        <div class="pe-icon-box"><i class="fa fa-sliders"></i></div>
        <div>
          <span class="pe-title-text">Premature Encashment Matrix</span>
          <span class="pe-page-subtitle">Yield-and-penalty rules for early withdrawal, by tenure</span>
        </div>
      </h4>
      <button class="pe-btn pe-btn-back" (click)="goBack()">
        <i class="fa fa-arrow-left"></i> Back
      </button>
    </div>

    <!-- Explainer banner -->
    <div class="pe-explainer">
      <i class="fa fa-shield"></i>
      <span>
        Final payout rate = <strong>Base yield %</strong> (auto-loaded from product tenure base rate) &minus; <strong>Net penalty %</strong>
        (slab penalty plus any customer-segment override &mdash; capped if a
        regulatory cap is enabled). Every rule-set you save is a dated <strong>version</strong>: it can start no
        earlier than tomorrow, and saving one that overlaps the active version automatically closes that version
        the day before yours begins. Past versions are never overwritten &mdash; they stay exactly as they were for audit.
      </span>
    </div>

    <!-- Product selection card -->
    <div class="pe-card mb-4">
      <div class="pe-card-header">
        <h5 class="pe-card-title"><i class="fa fa-cube"></i> Product selection</h5>
      </div>
      <div class="pe-card-body">
        <div class="row g-3 align-items-end">
          <div class="col-12 col-lg-6">
            <label class="pe-label">Select DPS product <span class="pe-required">*</span></label>
            <div class="pe-input-group">
              <span class="pe-input-icon"><i class="fa fa-cube"></i></span>
              <select class="pe-select" [value]="selectedProductId || ''" (change)="onProductSelect($event)">
                <option value="">Choose a product&hellip;</option>
                <option *ngFor="let prod of productList; trackBy: trackByProductId" [value]="prod.id">
                  {{ prod.code }} &ndash; {{ prod.name }}
                </option>
              </select>
            </div>
          </div>
          <div class="col-12 col-lg-6">
            <div class="pe-info-note">
              <i class="fa fa-info-circle"></i>
              Only DPS products are configured here. Each tenure keeps its own encashment matrix and full version history.
            </div>
          </div>
        </div>

        <div class="pe-product-detail fade-in" *ngIf="selectedProduct">
          <div class="row gy-3">
            <div class="col-md-3">
              <span class="pe-eyebrow">Product name</span>
              <p class="pe-value-strong">{{ selectedProduct.name }}</p>
            </div>
            <div class="col-md-2">
              <span class="pe-eyebrow">Product code</span>
              <p class="pe-value-strong mono pe-code-accent">{{ selectedProduct.code }}</p>
            </div>
            <div class="col-md-7">
              <span class="pe-eyebrow">Description</span>
              <p class="pe-value-muted">{{ selectedProduct.description }}</p>
            </div>
          </div>

          <div class="pe-deposit-config mt-3">
            <div class="pe-deposit-icon">
              <i class="fa" [ngClass]="selectedProduct.depositConfig.type === 'Fixed' ? 'fa-list-ol' : 'fa-arrows-h'"></i>
            </div>
            <div class="flex-grow-1">
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <span class="pe-deposit-title">Instalment amount</span>
                <span class="pe-chip" [ngClass]="selectedProduct.depositConfig.type === 'Fixed' ? 'pe-chip-purple' : 'pe-chip-green'">
                  {{ selectedProduct.depositConfig.type === 'Fixed' ? 'Fixed tiers' : 'Flexible range' }}
                </span>
                <span class="pe-chip pe-chip-blue" *ngFor="let f of selectedProduct.depositConfig.frequencies">
                  <i class="fa fa-clock-o"></i> {{ f }}
                </span>
              </div>

              <div class="mt-2" *ngIf="selectedProduct.depositConfig.type === 'Range'">
                <span class="mono pe-amount-strong">
                  {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(selectedProduct.depositConfig.minAmount!) }}
                  <i class="fa fa-long-arrow-right mx-1 pe-muted-icon"></i>
                  {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(selectedProduct.depositConfig.maxAmount!) }}
                </span>
                <span class="pe-value-muted small ms-2">
                  in steps of {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(selectedProduct.depositConfig.step!) }}
                </span>
              </div>

              <div class="mt-2 d-flex flex-wrap gap-2" *ngIf="selectedProduct.depositConfig.type === 'Fixed'">
                <span class="pe-tier-pill mono" *ngFor="let tier of selectedProduct.depositConfig.fixedOptions">
                  {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(tier) }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Stat strip -->
    <div class="pe-stat-strip mb-4" *ngIf="selectedProductId && !loading">
      <div class="pe-stat-card">
        <span class="pe-stat-label">Tenures shown</span>
        <span class="pe-stat-value">{{ statTotal }}</span>
      </div>
      <div class="pe-stat-card">
        <span class="pe-stat-label">With active rule-set</span>
        <span class="pe-stat-value pe-stat-positive">{{ statWithRule }}</span>
      </div>
      <div class="pe-stat-card">
        <span class="pe-stat-label">Scheduled changes ahead</span>
        <span class="pe-stat-value pe-stat-purple">{{ statScheduled }}</span>
      </div>
      <div class="pe-stat-card">
        <span class="pe-stat-label">Base rate range</span>
        <span class="pe-stat-value mono pe-stat-range">{{ statBaseRateRange }}</span>
      </div>
    </div>

    <!-- Filter bar -->
    <div class="pe-card pe-filter-card mb-3" *ngIf="selectedProductId">
      <div class="pe-filter-bar">
        <div class="pe-search">
          <i class="fa fa-search"></i>
          <input type="text" class="pe-search-input"
            [(ngModel)]="searchControl" [ngModelOptions]="{ standalone: true }"
            placeholder="Search by tenure name or code&hellip;" (keyup)="onSearch()" />
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div *ngIf="loading" class="pe-loading fade-in">
      <div class="spinner-border" role="status"><span class="sr-only">Loading&hellip;</span></div>
      <p>Building premature encashment matrix&hellip;</p>
    </div>

    <!-- Tenure cards -->
    <ng-container *ngIf="!loading && selectedProductId">
      <div class="pe-tenure-card" *ngFor="let t of filteredTenureList; trackBy: trackByTenureId">

        <div class="pe-tenure-header" [class.is-expanded]="t.isExpanded">
          <div class="pe-tenure-identity">
            <span class="pe-tenure-code">{{ t.code }}</span>
            <span class="pe-tenure-name">{{ t.durationName }}</span>
          </div>

          <div class="pe-base-rate">
            <span class="pe-base-rate-label">Base rate</span>
            <span class="pe-base-rate-value mono">{{ t.baseRate | number:'1.2-2' }}%</span>
          </div>

          <div class="pe-summary-pills" *ngIf="t.current as c; else noRule">
            <span class="pe-version-chip">v{{ c.version }} active</span>
            <span class="pe-pill"><i class="fa fa-clock-o"></i> {{ c.monthSlabs.length }} month slab{{ c.monthSlabs.length === 1 ? '' : 's' }}</span>

            <span class="pe-pill pe-pill-amber" *ngIf="c.exceptions.capEnabled"><i class="fa fa-shield"></i> Capped at {{ c.exceptions.capValue }}%</span>
            <span class="pe-pill pe-pill-purple" *ngIf="t.versions[0]?.isFuture">
              <i class="fa fa-clock-o"></i> Change scheduled {{ formatDate(t.versions[0].effectiveFrom) }}
            </span>
          </div>
          <ng-template #noRule>
            <div class="pe-summary-pills">
              <span class="pe-pill-empty"><i class="fa fa-exclamation-circle"></i> No active rule-set</span>
              <span class="pe-pill pe-pill-purple" *ngIf="t.versions.length && t.versions[0]?.isFuture">
                <i class="fa fa-clock-o"></i> Change scheduled {{ formatDate(t.versions[0].effectiveFrom) }}
              </span>
            </div>
          </ng-template>

          <div class="pe-tenure-actions">
            <button type="button" class="pe-btn pe-btn-ghost" (click)="toggleExpand(t)">
              <i class="fa" [ngClass]="t.isExpanded ? 'fa-chevron-up' : 'fa-history'"></i>
              {{ t.isExpanded ? 'Hide history' : 'View history' }}
              <span class="pe-count-pill" *ngIf="t.versions.length">{{ t.versions.length }}</span>
            </button>
            <button type="button" class="pe-btn pe-btn-primary" (click)="openEncashModal(t)">
              <i class="fa fa-plus"></i> Add rule
            </button>
          </div>
        </div>

        <!-- Expanded ledger — compact accordion, one line per version -->
        <div class="pe-ledger" *ngIf="t.isExpanded">
          <div class="pe-ledger-header">
            <h6><i class="fa fa-book"></i> Encashment rule ledger</h6>
            <span class="pe-ledger-count">{{ t.versions.length }} version{{ t.versions.length === 1 ? '' : 's' }} recorded for {{ t.durationName }}</span>
          </div>

          <details class="pe-version-row" *ngFor="let v of t.versions; trackBy: trackByVersionId"
            [open]="v.isCurrent" [class.is-current]="v.isCurrent">
            <summary class="pe-version-summary">
              <i class="fa fa-chevron-right pe-chevron"></i>
              <span class="pe-version-chip" [class.is-future]="v.isFuture" [class.is-expired]="v.isExpired">v{{ v.version }}</span>
              <span class="pe-status-tag" [ngClass]="{ 'is-current': v.isCurrent, 'is-future': v.isFuture, 'is-expired': v.isExpired }">
                <i class="fa" [ngClass]="{ 'fa-circle': v.isCurrent, 'fa-clock-o': v.isFuture, 'fa-check': v.isExpired }"></i>
                {{ v.isCurrent ? 'Active' : v.isFuture ? 'Scheduled' : 'Expired' }}
              </span>
              <span class="pe-version-dates mono">
                {{ formatDate(v.effectiveFrom) }} <i class="fa fa-long-arrow-right"></i> {{ v.effectiveTo ? formatDate(v.effectiveTo) : 'Ongoing' }}
              </span>
              <span class="pe-summary-rates mono">
                <i class="fa fa-line-chart pe-rate-pos"></i>{{ v.monthSlabs.length }} slab{{ v.monthSlabs.length === 1 ? '' : 's' }}
                <span class="pe-rate-sep">&middot;</span>
                {{ v.monthSlabs[0]?.baseYieldRate }}%<span class="pe-rate-sep">/</span>&minus;{{ v.monthSlabs[0]?.penaltyRate }}%
              </span>
              <span class="pe-summary-reason">{{ v.reasonForChange }}</span>
              <span class="pe-version-by">{{ v.createdBy || '—' }}</span>
            </summary>

            <div class="pe-version-detail">
              <table class="pe-matrix-table">
                <thead>
                  <tr>
                    <th>Months held</th>
                    <th class="text-center">Base yield</th>
                    <th class="text-center">Penalty</th>
                    <th class="text-center">Net payout</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let s of v.monthSlabs">
                    <td class="mono">{{ s.minMonths }}&ndash;{{ s.maxMonths }} mo</td>
                    <td class="text-center mono pe-rate-pos">{{ s.baseYieldRate }}%</td>
                    <td class="text-center mono pe-rate-neg">&minus;{{ s.penaltyRate }}%</td>
                    <td class="text-center mono pe-rate-net">{{ rowEffective(s.baseYieldRate, s.penaltyRate) }}%</td>
                  </tr>
                </tbody>
              </table>

              <div class="pe-detail-extras" *ngIf="v.segmentOverrides.length || v.exceptions.capEnabled || v.exceptions.waiveOnDeathClaim">
                <span class="pe-tag pe-tag-blue" *ngFor="let s of v.segmentOverrides" [class.is-zero]="s.penaltyAdjustment === 0">
                  {{ s.segment }}: {{ s.penaltyAdjustment > 0 ? '+' : '' }}{{ s.penaltyAdjustment }}%
                </span>
                <span class="pe-tag pe-tag-amber" *ngIf="v.exceptions.capEnabled">
                  <i class="fa fa-shield"></i> Capped {{ v.exceptions.capValue }}%
                </span>
                <span class="pe-tag pe-tag-amber" *ngIf="v.exceptions.waiveOnDeathClaim">
                  <i class="fa fa-heart-o"></i> Waived on death claim
                </span>
              </div>
            </div>
          </details>

          <div class="pe-no-versions" *ngIf="t.versions.length === 0">
            <i class="fa fa-exclamation-triangle"></i>
            <span>No encashment rules found for this tenure. Click "Add rule" to configure one.</span>
          </div>
        </div>
      </div>

      <div class="pe-empty-page" *ngIf="filteredTenureList.length === 0">
        <i class="fa fa-folder-open-o"></i>
        <p>No tenure configurations found for this product.</p>
      </div>
    </ng-container>

    <div *ngIf="!selectedProductId && !loading" class="pe-empty-initial fade-in">
      <div class="pe-empty-icon-wrap"><i class="fa fa-sliders"></i></div>
      <h5>No product selected</h5>
      <p>Select a DPS product from the dropdown above to view its encashment matrices.</p>
    </div>

  </div>
</section>

<!-- ======================================================================
     ADD NEW ENCASHMENT RULE MODAL
     ====================================================================== -->
<ng-template #encashModal let-modal>
  <div class="pe-modal-content" *ngIf="activeTenure && selectedProduct">

    <div class="pe-modal-header">
      <div class="d-flex align-items-center gap-3">
        <div class="pe-modal-icon-box"><i class="fa fa-sliders"></i></div>
        <div>
          <h5 class="pe-modal-title">Add new encashment rule</h5>
          <span class="pe-modal-subtitle">
            <i class="fa fa-clock-o"></i> {{ activeTenure.durationName }} &middot; {{ activeTenure.code }}
            <span class="pe-version-chip pe-version-chip-light">will be v{{ activeTenure.versions.length + 1 }}</span>
          </span>
        </div>
      </div>
      <button type="button" class="pe-modal-close" (click)="closeEncashModal()"><i class="fa fa-times"></i></button>
    </div>

    <form [formGroup]="matrixForm" class="pe-modal-body">

      <!-- Context strip -->
      <div class="pe-context-strip mb-4">
        <div class="pe-context-chip">
          <span class="pe-eyebrow"><i class="fa fa-cube"></i> Product</span>
          <span class="pe-context-value">{{ selectedProduct.code }} &middot; {{ selectedProduct.name }}</span>
        </div>
        <div class="pe-context-chip">
          <span class="pe-eyebrow"><i class="fa fa-clock-o"></i> Tenure</span>
          <span class="pe-context-value">{{ activeTenure.durationName }}</span>
        </div>
        <div class="pe-context-chip pe-context-gold">
          <span class="pe-eyebrow"><i class="fa fa-percent"></i> Base rate</span>
          <span class="pe-context-value mono">{{ activeTenure.baseRate | number:'1.2-2' }}%</span>
        </div>
        <div class="pe-context-chip">
          <span class="pe-eyebrow"><i class="fa fa-refresh"></i> Frequency</span>
          <span class="pe-context-value">{{ selectedProduct.depositConfig.frequencies.join(', ') }}</span>
        </div>
        <div class="pe-context-chip" *ngIf="selectedProduct.depositConfig.type === 'Range'">
          <span class="pe-eyebrow"><i class="fa fa-money"></i> Amount range</span>
          <span class="pe-context-value mono">
            {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(selectedProduct.depositConfig.minAmount!) }}
            &ndash;
            {{ selectedProduct.depositConfig.symbol }}{{ formatAmount(selectedProduct.depositConfig.maxAmount!) }}
          </span>
        </div>
        <div class="pe-context-chip" *ngIf="selectedProduct.depositConfig.type === 'Fixed'">
          <span class="pe-eyebrow"><i class="fa fa-tags"></i> Fixed tiers</span>
          <span class="pe-context-value mono">{{ selectedProduct.depositConfig.fixedOptions?.join(', ') }}</span>
        </div>
      </div>

      <!-- Previous version -->
      <div class="pe-prev-version mb-4" *ngIf="activeTenure.current as prev">
        <div class="pe-prev-heading">
          <i class="fa fa-history"></i> Previous version &middot; v{{ prev.version }}
          <span class="pe-prev-since">active since {{ formatDate(prev.effectiveFrom) }}</span>
        </div>
        <div class="pe-prev-rows">
          <span class="pe-tag" *ngFor="let s of prev.monthSlabs">
            {{ s.minMonths }}&ndash;{{ s.maxMonths }}mo: {{ s.baseYieldRate }}% / &minus;{{ s.penaltyRate }}%
          </span>

          <span class="pe-tag pe-tag-blue" *ngFor="let s of prev.segmentOverrides" [class.is-zero]="s.penaltyAdjustment === 0">
            {{ s.segment }}: {{ s.penaltyAdjustment > 0 ? '+' : '' }}{{ s.penaltyAdjustment }}%
          </span>
          <span class="pe-tag pe-tag-amber" *ngIf="prev.exceptions.capEnabled">
            <i class="fa fa-shield"></i> Capped {{ prev.exceptions.capValue }}%
          </span>
        </div>
      </div>
      <div class="pe-prev-version pe-prev-empty mb-4" *ngIf="!activeTenure.current">
        <i class="fa fa-info-circle"></i> This tenure has no active rule-set yet — this will be the first version.
      </div>

      <!-- Date range -->
      <div class="pe-section-card mb-4">
        <div class="row g-3 align-items-start">
          <div class="col-md-5">
            <label class="pe-label">Effective from <span class="pe-required">*</span></label>
            <div class="pe-input-group">
              <span class="pe-input-icon"><i class="fa fa-calendar"></i></span>
              <input type="date" class="pe-input" formControlName="effectiveFrom" [min]="minSelectableDate"
                [ngClass]="{ 'is-invalid': matrixForm.get('effectiveFrom')?.touched && matrixForm.get('effectiveFrom')?.errors }" />
            </div>
            <div *ngIf="matrixForm.get('effectiveFrom')?.touched && matrixForm.get('effectiveFrom')?.errors" class="pe-invalid-feedback">
              Effective-from date is required.
            </div>
            <span class="pe-hint">
              <i class="fa fa-lock"></i> Earliest allowed start: {{ formatDate(minSelectableDate!) }} (today cannot be selected)
            </span>
          </div>
          <div class="col-md-4">
            <div class="pe-switch-row">
              <label class="pe-switch">
                <input type="checkbox" id="hasEndDateEnc" formControlName="hasEndDate" />
                <span class="pe-switch-track"></span>
              </label>
              <label for="hasEndDateEnc" class="pe-switch-label">Has a known end date</label>
            </div>
            <span class="pe-hint pe-hint-plain">Leave unchecked to keep this version open-ended.</span>
          </div>
          <div class="col-md-3" *ngIf="hasEndDateChecked">
            <label class="pe-label">Effective to <span class="pe-required">*</span></label>
            <div class="pe-input-group">
              <span class="pe-input-icon"><i class="fa fa-calendar-check-o"></i></span>
              <input type="date" class="pe-input" formControlName="effectiveTo" [min]="minSelectableDate" />
            </div>
          </div>
        </div>
        <div class="pe-notice pe-notice-amber mt-3" *ngIf="supersedeNotice">
          <i class="fa fa-magic"></i> {{ supersedeNotice }}
        </div>
        <div class="pe-notice pe-notice-red mt-3" *ngIf="dateOverlapWarning">
          <i class="fa fa-exclamation-triangle"></i> {{ dateOverlapWarning }}
        </div>
      </div>

      <!-- Formula banner -->
      <div class="pe-formula-banner mb-4">
        <i class="fa fa-calculator"></i>
        <span>Final payout rate = <strong>Base yield %</strong> (auto-loaded from tenure base rate &mdash; read only) &minus; <strong>Net penalty %</strong> (slab penalty + segment override, capped if enabled)</span>
      </div>

      <!-- Yield/penalty slabs -->
      <div class="pe-rule-card pe-rule-primary mb-4">
        <div class="pe-rule-header">
          <span><i class="fa fa-line-chart"></i> Penalty by months held</span>
          <div class="d-flex align-items-center gap-3">
            <span class="pe-base-rate-info">
              <i class="fa fa-lock"></i>
              Base yield: <strong class="mono">{{ activeTenure?.baseRate | number:'1.2-2' }}%</strong>
              <span class="pe-hint ms-1">(auto-loaded from tenure &mdash; read only)</span>
            </span>
            <button type="button" class="pe-btn-add" (click)="addMonthSlab()">
              <i class="fa fa-plus"></i> Add slab
            </button>
          </div>
        </div>
        <p class="pe-rule-sub">The base yield is fixed to the tenure base rate. Configure the penalty progression as the deposit approaches full term.</p>

        <div formArrayName="monthSlabs">
          <div class="pe-slab-grid pe-slab-grid-header">
            <div>#</div>
            <div>From (mo)</div>
            <div>To (mo)</div>
            <div>Base yield %</div>
            <div>Penalty %</div>
            <div>Net payout</div>
            <div></div>
          </div>

          <div *ngFor="let s of monthSlabs.controls; let si = index"
            [formGroupName]="si" class="pe-slab-grid pe-slab-grid-row" [class.is-invalid-row]="s.invalid && s.touched">
            <div class="pe-cell-num mono">{{ si + 1 }}</div>
            <div><input type="number" class="pe-input pe-input-sm text-center" formControlName="minMonths" /></div>
            <div><input type="number" class="pe-input pe-input-sm text-center" formControlName="maxMonths" /></div>
            <div class="pe-input-group pe-input-group-sm">
              <input type="number" step="0.05" class="pe-input pe-input-sm text-center pe-input-readonly"
                formControlName="baseYieldRate" readonly tabindex="-1" />
              <span class="pe-input-suffix">%</span>
            </div>
            <div class="pe-input-group pe-input-group-sm">
              <input type="number" step="0.05" class="pe-input pe-input-sm text-center" formControlName="penaltyRate" />
              <span class="pe-input-suffix">%</span>
            </div>
            <div class="text-center">
              <span class="pe-payout-badge">{{ rowEffective(s.get('baseYieldRate')?.value, s.get('penaltyRate')?.value) }}%</span>
            </div>
            <div class="text-end">
              <button type="button" class="pe-btn-remove" (click)="removeMonthSlab(si)" [disabled]="monthSlabs.length <= 1">
                <i class="fa fa-trash-o"></i>
              </button>
            </div>
          </div>
        </div>

        <div class="pe-notice pe-notice-red" *ngIf="monthSlabWarning">
          <i class="fa fa-exclamation-triangle"></i> {{ monthSlabWarning }}
        </div>
      </div>

      <!-- Segment overrides + Exceptions -->
      <div class="row g-4 mb-4">
        <div class="col-lg-7">
          <div class="pe-rule-card pe-rule-segment h-100">
            <div class="pe-rule-header">
              <span><i class="fa fa-users"></i> Customer segment overrides</span>
              <button type="button" class="pe-btn-add" (click)="addSegmentOverride()"
                [disabled]="segmentOverrides.length >= standardSegments.length">
                <i class="fa fa-plus"></i> Add override
              </button>
            </div>
            <p class="pe-rule-sub">Adjustment is added to the penalty. Negative values are favorable to the customer.</p>

            <div formArrayName="segmentOverrides">
              <div class="pe-seg-grid pe-seg-grid-header" *ngIf="segmentOverrides.length > 0">
                <div>Segment</div>
                <div>Penalty adjustment</div>
                <div></div>
              </div>
              <div *ngFor="let s of segmentOverrides.controls; let segi = index"
                [formGroupName]="segi" class="pe-seg-grid pe-seg-grid-row">
                <div>
                  <select class="pe-select pe-select-sm" formControlName="segment">
                    <option value="" disabled>Select segment&hellip;</option>
                    <option *ngFor="let opt of availableSegments(segi)" [value]="opt">{{ opt }}</option>
                    <option *ngIf="s.get('segment')?.value && !availableSegments(segi).includes(s.get('segment')?.value)"
                      [value]="s.get('segment')?.value">{{ s.get('segment')?.value }}</option>
                  </select>
                </div>
                <div class="pe-input-group pe-input-group-sm">
                  <input type="number" step="0.05" class="pe-input pe-input-sm text-center"
                    formControlName="penaltyAdjustment"
                    [ngClass]="{ 'pe-text-pos': isNegative(s.get('penaltyAdjustment')?.value), 'pe-text-neg': isPositive(s.get('penaltyAdjustment')?.value) }" />
                  <span class="pe-input-suffix">%</span>
                </div>
                <div class="text-end">
                  <button type="button" class="pe-btn-remove" (click)="removeSegmentOverride(segi)">
                    <i class="fa fa-trash-o"></i>
                  </button>
                </div>
              </div>
              <div class="pe-empty-note" *ngIf="segmentOverrides.length === 0">
                <i class="fa fa-info-circle"></i> No overrides added yet.
                <a href="javascript:void(0)" (click)="addSegmentOverride()" class="pe-link">Add first override.</a>
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-5">
          <div class="pe-rule-card pe-rule-exception h-100">
            <div class="pe-rule-header">
              <span><i class="fa fa-shield"></i> Exceptions and regulatory caps</span>
            </div>
            <div class="pe-exception-row">
              <label class="pe-switch">
                <input type="checkbox" id="waiveOnDeath" formControlName="waiveOnDeathClaim" />
                <span class="pe-switch-track"></span>
              </label>
              <label for="waiveOnDeath" class="pe-switch-label">Waive penalty entirely on death claim settlement</label>
            </div>
            <div class="pe-exception-row">
              <label class="pe-switch">
                <input type="checkbox" id="capEnabled" formControlName="capEnabled" />
                <span class="pe-switch-track"></span>
              </label>
              <label for="capEnabled" class="pe-switch-label">Cap total penalty regardless of slab, at</label>
              <div class="pe-input-group pe-input-group-sm pe-cap-input" *ngIf="capEnabledChecked">
                <input type="number" step="0.05" class="pe-input pe-input-sm text-center" formControlName="capValue" />
                <span class="pe-input-suffix">%</span>
              </div>
            </div>
           
          </div>
        </div>
      </div>

      <!-- Reason -->
      <div class="pe-reason-section">
        <label class="pe-label">Reason for this change <span class="pe-required">*</span></label>
        <div class="pe-quick-reasons mb-3">
          <button type="button" class="pe-reason-chip" *ngFor="let qr of quickReasons" (click)="applyQuickReason(qr)">
            {{ qr }}
          </button>
        </div>
        <textarea rows="2" class="pe-textarea" formControlName="reasonForChange"
          placeholder="e.g. Treasury circular TC-2026-09 — widened the senior-citizen waiver"
          [ngClass]="{ 'is-invalid': matrixForm.get('reasonForChange')?.touched && matrixForm.get('reasonForChange')?.errors }">
        </textarea>
        <div *ngIf="matrixForm.get('reasonForChange')?.touched && matrixForm.get('reasonForChange')?.errors" class="pe-invalid-feedback">
          A reason is required for audit purposes.
        </div>
      </div>
    </form>

    <div class="pe-modal-footer">
      <button type="button" class="pe-btn pe-btn-cancel" (click)="closeEncashModal()">Cancel</button>
      <button type="button" class="pe-btn pe-btn-save" (click)="submitEncashmentRule()"
        [disabled]="submitting || !!monthSlabWarning || !!dateOverlapWarning">
        <i class="fa" [ngClass]="submitting ? 'fa-spinner fa-spin' : 'fa-save'"></i>
        {{ submitting ? 'Saving…' : 'Save rule' }}
      </button>
    </div>
  </div>
</ng-template>










import { ChangeDetectorRef, Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DatePipe, Location } from '@angular/common';
import { SharedService } from 'app/services/shared.service';

import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import Swal from 'sweetalert2';
import { LoanProductService } from 'app/product/service/loan-product.service';

// =============================================================================
// Models
// =============================================================================
//
// NOTE ON MOCK DATA
// -----------------------------------------------------------------------------
// Everything under "MOCK DATA SOURCE" at the bottom of this file stands in for
// real API calls. Each mock method's name matches a future service call 1:1
// (e.g. getDummyProducts() -> service.getDpsProducts()), and every model below
// mirrors the shape we expect the API to return. Swap the method body for a
// real `this.service.xxx().subscribe(...)` call and nothing else needs to change.
// =============================================================================
export interface PrematureEncashmentRateVM {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  tenorId: number;
  durationName?: string;
  reasonForChange: string;
  rate: number; // EncashRate in DB
  effectiveFrom: string | Date;
  effectiveTo: string | Date;
  isActive: boolean;
  monthSlabs: MonthSlab[];
  amountSlabs: AmountSlabVM[];
}
export type DepositType = 'Fixed' | 'Range';

export interface DpsDepositConfig {
  type: DepositType;
  currency: string;
  symbol: string;
  frequencies: string[];
  fixedOptions?: number[];
  minAmount?: number;
  maxAmount?: number;
  step?: number;
}

export interface DpsProduct {
  id: number;
  name: string;
  code: string;
  category: 'DPS';
  description: string;
  depositConfig: DpsDepositConfig;
}

export interface TenureDuration {
  id: number;
  productId: number;
  code: string;
  durationName: string;
  months: number;
  baseRate: number;
}
export interface AmountSlabVM {
  minPrincipal: number;
  maxPrincipal: number;
  penaltyRate: number;
}
/** Primary driver: yield + penalty progression by completed months held. */
export interface MonthSlab {
  id: number;
  minMonths: number;
  maxMonths: number;
  baseYieldRate: number;
  penaltyRate: number;
}

/** Secondary, opt-in driver: extra penalty modifier by absolute principal size. */
export interface PrincipalModifierSlab {
  id: number;
  principalFrom: number;
  principalTo: number;
  modifier: number;
}

/** Fixed list of customer segments — consistent across every tenure and version. */
export const STANDARD_SEGMENTS = ['Regular', 'Senior citizen', 'Staff / employee', 'Premium / HNI'] as const;
export type StandardSegment = typeof STANDARD_SEGMENTS[number];

export interface SegmentOverride {
  segment: string;
  penaltyAdjustment: number;
}

export interface EncashmentExceptions {
  waiveOnDeathClaim: boolean;
  capEnabled: boolean;
  capValue: number;
  allowPartialEncashment: boolean;
}

/**
 * One full, dated rule-set for a tenure — months table, principal modifier,
 * segment overrides, and exceptions all change together as a single version,
 * the same way TenureRateHistory / AdjustmentVersion bundle a whole rule
 * payload to one effective date window.
 */
export interface EncashmentVersion {
  id: number;
  tenorId: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  reasonForChange: string;
  monthSlabs: MonthSlab[];
  principalModifierEnabled: boolean;
  principalModifierSlabs: PrincipalModifierSlab[];
  segmentOverrides: SegmentOverride[];
  exceptions: EncashmentExceptions;
  createdAt?: string;
  createdBy?: string;
}

/** An EncashmentVersion enriched with the computed, display-only fields the template needs. */
export interface EncashmentVersionRecord extends EncashmentVersion {
  version: number;
  isCurrent: boolean;
  isFuture: boolean;
  isExpired: boolean;
}

export interface TenureWithEncashment extends TenureDuration {
  versions: EncashmentVersionRecord[];
  current: EncashmentVersionRecord | null;
  isExpanded?: boolean;
}

@Component({
  selector: 'app-premature-encashment-rate',
  templateUrl: './premature-encashment-rate.component.html',
  styleUrls: ['./premature-encashment-rate.component.scss'],
  providers: [DatePipe]
})
export class PrematureEncashmentRateComponent implements OnInit {
  @ViewChild('encashModal') encashModal: TemplateRef<any>;
  modalRef: NgbModalRef;

  readonly standardSegments = STANDARD_SEGMENTS;

  // ---------------------------------------------------------------------
  // Product + tenure state
  // ---------------------------------------------------------------------

  productList: DpsProduct[] = [];
  selectedProductId: number | null = null;
  selectedProduct: DpsProduct | null = null;

  searchControl = '';
  tenureList: TenureWithEncashment[] = [];
  filteredTenureList: TenureWithEncashment[] = [];
  loading = false;

  // ---------------------------------------------------------------------
  // Add-encashment-rule modal state
  // ---------------------------------------------------------------------

  matrixForm: FormGroup;
  submitting = false;
  activeTenure: TenureWithEncashment | null = null;

  monthSlabWarning: string | null = null;
  principalSlabWarning: string | null = null;
  dateOverlapWarning: string | null = null;
  supersedeNotice: string | null = null;
  minSelectableDate: string | null = null;

  quickReasons: string[] = [
    'Treasury circular',
    'Quarterly penalty revision',
    'Promotional waiver campaign',
    'Annual yearly review',
    'Regulatory cap adjustment'
  ];

  constructor(
    private fb: FormBuilder,
    private shared: SharedService,
    private service: LoanProductService,
    private datePipe: DatePipe,
    private cdr: ChangeDetectorRef,
    private location: Location,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  // =========================================================================
  // Product selection
  // =========================================================================

  private loadProducts(): void {
    // Swap for: this.service.getDpsProducts().subscribe(list => this.productList = list);
    this.productList = this.getDummyProducts();
  }

  onProductSelect(event: any): void {
    const id = +event.target.value;
    if (!id) {
      this.selectedProductId = null;
      this.selectedProduct = null;
      this.tenureList = [];
      this.filteredTenureList = [];
      return;
    }
    this.selectedProductId = id;
    this.selectedProduct = this.productList.find(p => p.id === id) || null;
    this.searchControl = '';
    this.loadTenureList();
  }

  loadTenureList(): void {
    if (!this.selectedProductId) return;

    this.loading = true;
    this.tenureList = [];
    this.filteredTenureList = [];

    setTimeout(() => {
      // Swap for: this.service.getTenuresForProduct(id) + this.service.getEncashmentVersions(...)
      const tenures = this.getDummyTenures().filter(t => t.productId === this.selectedProductId);
      const versions = this.getDummyEncashmentVersions();

      this.tenureList = tenures.map(t => this.attachEncashmentHistory(t, versions));
      this.applyFilters();
      this.loading = false;
      this.cdr.detectChanges();
    }, 450);
  }

  /** The effective min/max balance this product allows, regardless of Fixed vs Range — shown for context only on this page. */
  amountBoundsFor(product: DpsProduct): { min: number; max: number } | null {
    const cfg = product.depositConfig;
    if (cfg.type === 'Range' && cfg.minAmount != null && cfg.maxAmount != null) {
      return { min: cfg.minAmount, max: cfg.maxAmount };
    }
    if (cfg.type === 'Fixed' && cfg.fixedOptions?.length) {
      return { min: Math.min(...cfg.fixedOptions), max: Math.max(...cfg.fixedOptions) };
    }
    return null;
  }

  // =========================================================================
  // Derived per-tenure state (versioning, same shape as DpsInterestAdjustConfigComponent)
  // =========================================================================

  private attachEncashmentHistory(
    tenure: TenureDuration,
    allVersions: EncashmentVersion[]
  ): TenureWithEncashment {
    const today = this.todayIso();
    const raw = allVersions.filter(v => v.tenorId === tenure.id);

    const chronological = [...raw].sort((a, b) => a.effectiveFrom.localeCompare(b.effectiveFrom));
    const versionOf = new Map<number, number>();
    chronological.forEach((v, idx) => versionOf.set(v.id, idx + 1));

    const versions: EncashmentVersionRecord[] = raw
      .map(v => ({
        ...v,
        version: versionOf.get(v.id) as number,
        isCurrent: v.effectiveFrom <= today && (!v.effectiveTo || v.effectiveTo >= today),
        isFuture: v.effectiveFrom > today,
        isExpired: !!v.effectiveTo && v.effectiveTo < today
      }))
      .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom));

    const current = versions.find(v => v.isCurrent) || null;

    return { ...tenure, versions, current, isExpanded: false };
  }

  toggleExpand(t: TenureWithEncashment): void {
    t.isExpanded = !t.isExpanded;
  }

  applyFilters(): void {
    const q = this.searchControl.toLowerCase().trim();
    this.filteredTenureList = this.tenureList.filter(
      t => !q || t.durationName.toLowerCase().includes(q) || t.code.toLowerCase().includes(q)
    );
  }

  onSearch(): void {
    this.applyFilters();
  }

  // =========================================================================
  // Stat strip
  // =========================================================================

  get statTotal(): number {
    return this.filteredTenureList.length;
  }

  get statWithRule(): number {
    return this.filteredTenureList.filter(t => !!t.current).length;
  }

  get statScheduled(): number {
    return this.filteredTenureList.filter(t => t.versions.some(v => v.isFuture)).length;
  }

  get statBaseRateRange(): string {
    const bases = this.filteredTenureList.map(t => t.baseRate);
    if (!bases.length) return '—';
    const lo = Math.min(...bases).toFixed(2);
    const hi = Math.max(...bases).toFixed(2);
    return lo === hi ? `${lo}%` : `${lo}% – ${hi}%`;
  }

  // =========================================================================
  // Calculation engine — the same logic the ledger preview uses
  // =========================================================================

  /**
   * Net penalty for a given completed-months point and (optionally) a principal
   * size and segment, given one version's full rule-set.
   * netPenalty = slabPenalty + (principal modifier, if enabled) + segment override,
   * then capped at capValue if a cap is enabled.
   */
  netPenaltyFor(
    version: EncashmentVersion,
    months: number,
    principal: number | null = null,
    segment: string | null = null
  ): number {
    const slab = version.monthSlabs.find(s => months >= s.minMonths && months <= s.maxMonths);
    let net = slab ? slab.penaltyRate : 0;

    if (version.principalModifierEnabled && principal !== null) {
      const pSlab = version.principalModifierSlabs.find(
        p => principal >= p.principalFrom && principal <= p.principalTo
      );
      if (pSlab) net += pSlab.modifier;
    }

    if (segment) {
      const ov = version.segmentOverrides.find(s => s.segment === segment);
      if (ov) net += ov.penaltyAdjustment;
    }

    if (version.exceptions.capEnabled) {
      net = Math.min(net, version.exceptions.capValue);
    }

    return Math.round(net * 100) / 100;
  }

  /** Final payout rate = base yield (for the months slab) - net penalty. */
  payoutRateFor(
    version: EncashmentVersion,
    months: number,
    principal: number | null = null,
    segment: string | null = null
  ): number {
    const slab = version.monthSlabs.find(s => months >= s.minMonths && months <= s.maxMonths);
    const baseYield = slab ? slab.baseYieldRate : 0;
    const net = this.netPenaltyFor(version, months, principal, segment);
    return Math.round((baseYield - net) * 100) / 100;
  }

  /** Live per-row preview inside the modal table: payout = base yield - that row's own penalty (slab-only, before principal/segment stacking). */
  rowEffective(baseYield: number, penaltyRate: number): number {
    return Math.round(((+baseYield || 0) - (+penaltyRate || 0)) * 100) / 100;
  }

  // =========================================================================
  // Add-encashment-rule modal
  // =========================================================================

  openEncashModal(tenure: TenureWithEncashment): void {
    if (!this.selectedProduct) return;

    this.activeTenure = tenure;
    this.monthSlabWarning = null;
    this.principalSlabWarning = null;
    this.dateOverlapWarning = null;
    this.supersedeNotice = null;

    // Earliest selectable start is tomorrow — today and any past date are blocked outright.
    this.minSelectableDate = this.addDays(this.todayIso(), 1);

    const previous = tenure.current;

    const tenureBaseRate = tenure.baseRate;

    const monthSlabRows = (previous?.monthSlabs.length ? previous.monthSlabs : this.defaultMonthSlabs()).map(s =>
      this.fb.group(
        {
          minMonths: [s.minMonths, [Validators.required, Validators.min(0)]],
          maxMonths: [s.maxMonths, [Validators.required, Validators.min(0)]],
          // baseYieldRate is always auto-loaded from the tenure base rate — user cannot modify it
          baseYieldRate: [{ value: tenureBaseRate, disabled: false }, [Validators.required, Validators.min(0)]],
          penaltyRate: [s.penaltyRate, [Validators.required, Validators.min(0)]]
        },
        { validators: this.rangeValidator('minMonths', 'maxMonths') }
      )
    );

    const principalRows = (previous?.principalModifierSlabs || []).map(p =>
      this.fb.group(
        {
          principalFrom: [p.principalFrom, [Validators.required, Validators.min(0)]],
          principalTo: [p.principalTo, [Validators.required, Validators.min(0)]],
          modifier: [p.modifier, Validators.required]
        },
        { validators: this.rangeValidator('principalFrom', 'principalTo') }
      )
    );

    // Start from previous overrides only; user adds rows dynamically via dropdown
    const segmentRows = (previous?.segmentOverrides ?? []).map(o =>
      this.fb.group({
        segment: [o.segment, Validators.required],
        penaltyAdjustment: [o.penaltyAdjustment, Validators.required]
      })
    );

    this.matrixForm = this.fb.group({
      effectiveFrom: [this.minSelectableDate, Validators.required],
      hasEndDate: [false],
      effectiveTo: [''],
      reasonForChange: ['', Validators.required],
      monthSlabs: this.fb.array(monthSlabRows),
      principalModifierEnabled: [previous?.principalModifierEnabled || false],
      principalModifierSlabs: this.fb.array(principalRows),
      segmentOverrides: this.fb.array(segmentRows),
      waiveOnDeathClaim: [previous?.exceptions.waiveOnDeathClaim ?? true],
      capEnabled: [previous?.exceptions.capEnabled ?? false],
      capValue: [previous?.exceptions.capValue ?? 0],
      allowPartialEncashment: [previous?.exceptions.allowPartialEncashment ?? true]
    });

    this.matrixForm.get('effectiveFrom')!.valueChanges.subscribe(() => this.checkDateOverlap());
    this.matrixForm.get('effectiveTo')!.valueChanges.subscribe(() => this.checkDateOverlap());
    this.matrixForm.get('hasEndDate')!.valueChanges.subscribe(checked => {
      if (!checked) this.matrixForm.patchValue({ effectiveTo: '' }, { emitEvent: false });
      this.checkDateOverlap();
    });
    this.monthSlabs.valueChanges.subscribe(() => this.checkMonthSlabOverlap());
    this.principalModifierSlabs.valueChanges.subscribe(() => this.checkPrincipalSlabOverlap());

    this.checkDateOverlap();
    this.checkMonthSlabOverlap();

    // Disable baseYieldRate in all slab rows — it is auto-loaded from the tenure base rate
    this.monthSlabs.controls.forEach(ctrl => ctrl.get('baseYieldRate')?.disable());

    this.modalRef = this.modalService.open(this.encashModal, {
      size: 'xl',
      backdrop: 'static',
      keyboard: false,
      centered: true,
      windowClass: 'adjust-matrix-modal'
    });
  }

  closeEncashModal(): void {
    if (this.modalRef) this.modalRef.dismiss();
    this.activeTenure = null;
    this.monthSlabWarning = null;
    this.principalSlabWarning = null;
    this.dateOverlapWarning = null;
    this.supersedeNotice = null;
  }

  get monthSlabs(): FormArray {
    return this.matrixForm.get('monthSlabs') as FormArray;
  }

  get principalModifierSlabs(): FormArray {
    return this.matrixForm.get('principalModifierSlabs') as FormArray;
  }

  get segmentOverrides(): FormArray {
    return this.matrixForm.get('segmentOverrides') as FormArray;
  }

  get hasEndDateChecked(): boolean {
    return this.matrixForm.get('hasEndDate')!.value === true;
  }

  get principalModifierEnabledChecked(): boolean {
    return this.matrixForm.get('principalModifierEnabled')!.value === true;
  }

  get capEnabledChecked(): boolean {
    return this.matrixForm.get('capEnabled')!.value === true;
  }

  addMonthSlab(): void {
    const tenureBaseRate = this.activeTenure?.baseRate ?? 0;
    const group = this.fb.group(
      {
        minMonths: [0, [Validators.required, Validators.min(0)]],
        maxMonths: [0, [Validators.required, Validators.min(0)]],
        // Auto-load from tenure base rate; user cannot modify
        baseYieldRate: [tenureBaseRate, [Validators.required, Validators.min(0)]],
        penaltyRate: [0, [Validators.required, Validators.min(0)]]
      },
      { validators: this.rangeValidator('minMonths', 'maxMonths') }
    );
    group.get('baseYieldRate')?.disable();
    this.monthSlabs.push(group);
  }

  removeMonthSlab(index: number): void {
    if (this.monthSlabs.length <= 1) return;
    this.monthSlabs.removeAt(index);
    this.checkMonthSlabOverlap();
  }

  addPrincipalSlab(): void {
    this.principalModifierSlabs.push(
      this.fb.group(
        {
          principalFrom: [0, [Validators.required, Validators.min(0)]],
          principalTo: [0, [Validators.required, Validators.min(0)]],
          modifier: [0, Validators.required]
        },
        { validators: this.rangeValidator('principalFrom', 'principalTo') }
      )
    );
  }

  removePrincipalSlab(index: number): void {
    this.principalModifierSlabs.removeAt(index);
    this.checkPrincipalSlabOverlap();
  }

  addSegmentOverride(): void {
    this.segmentOverrides.push(
      this.fb.group({
        segment: ['', Validators.required],
        penaltyAdjustment: [0, Validators.required]
      })
    );
  }

  removeSegmentOverride(index: number): void {
    this.segmentOverrides.removeAt(index);
  }

  /** Returns segments not yet selected in other rows (to avoid duplicates in dropdown options). */
  availableSegments(currentIndex: number): readonly string[] {
    const taken = this.segmentOverrides.controls
      .map((c, i) => i !== currentIndex ? c.get('segment')?.value : null)
      .filter(Boolean);
    return this.standardSegments.filter(s => !taken.includes(s));
  }

  applyQuickReason(reason: string): void {
    this.matrixForm.patchValue({ reasonForChange: reason });
    this.matrixForm.get('reasonForChange')!.markAsTouched();
  }

  // -------------------------------------------------------------------------
  // Validation: month-slab range/overlap, principal-slab range/overlap, dates
  // -------------------------------------------------------------------------

  private rangeValidator(minKey: string, maxKey: string) {
    return (control: any) => {
      const min = control.get(minKey)?.value;
      const max = control.get(maxKey)?.value;
      return min !== null && max !== null && +min >= +max ? { invalidRange: true } : null;
    };
  }

  private checkMonthSlabOverlap(): void {
    this.monthSlabWarning = null;
    const rows = this.monthSlabs.value as Array<{ minMonths: number; maxMonths: number }>;

    for (let i = 0; i < rows.length; i++) {
      const a = rows[i];
      if (+a.minMonths >= +a.maxMonths) {
        this.monthSlabWarning = `Row ${i + 1}: "From" must be less than "To".`;
        return;
      }
      for (let j = i + 1; j < rows.length; j++) {
        const b = rows[j];
        if (+a.minMonths < +b.maxMonths && +b.minMonths < +a.maxMonths) {
          this.monthSlabWarning = `Rows ${i + 1} and ${j + 1} overlap. Each month range must be exclusive.`;
          return;
        }
      }
    }
  }

  private checkPrincipalSlabOverlap(): void {
    this.principalSlabWarning = null;
    if (!this.principalModifierEnabledChecked) return;
    const rows = this.principalModifierSlabs.value as Array<{ principalFrom: number; principalTo: number }>;

    for (let i = 0; i < rows.length; i++) {
      const a = rows[i];
      if (+a.principalFrom >= +a.principalTo) {
        this.principalSlabWarning = `Row ${i + 1}: "Principal from" must be less than "Principal to".`;
        return;
      }
      for (let j = i + 1; j < rows.length; j++) {
        const b = rows[j];
        if (+a.principalFrom < +b.principalTo && +b.principalFrom < +a.principalTo) {
          this.principalSlabWarning = `Rows ${i + 1} and ${j + 1} overlap. Each principal band must be exclusive.`;
          return;
        }
      }
    }
  }

  /**
   * Date validation mirrors DpsInterestAdjustConfigComponent exactly: earliest
   * start is tomorrow, overlapping the active version auto-supersedes it the
   * day before the new one begins, overlapping expired/future is blocked.
   */
  private checkDateOverlap(): void {
    this.dateOverlapWarning = null;
    this.supersedeNotice = null;
    if (!this.activeTenure || !this.minSelectableDate) return;

    const from = this.matrixForm.get('effectiveFrom')!.value;
    const hasEndDate = this.matrixForm.get('hasEndDate')!.value;
    const to = hasEndDate ? this.matrixForm.get('effectiveTo')!.value : null;
    if (!from) return;

    if (from < this.minSelectableDate) {
      this.dateOverlapWarning = `The earliest start date allowed is ${this.formatDate(this.minSelectableDate)} — today's date cannot be selected.`;
      return;
    }

    if (to && from > to) {
      this.dateOverlapWarning = 'Effective-to date must be after the effective-from date.';
      return;
    }

    const newStart = from;
    const newEnd = to || '9999-12-31';

    for (const existing of this.activeTenure.versions) {
      const exEnd = existing.effectiveTo || '9999-12-31';
      const overlaps = newStart <= exEnd && existing.effectiveFrom <= newEnd;
      if (!overlaps) continue;

      if (existing.isCurrent) {
        this.supersedeNotice =
          `This will automatically close the active rule-set v${existing.version} ` +
          `(running since ${this.formatDate(existing.effectiveFrom)}) on ${this.formatDate(this.previousDay(newStart))}.`;
        continue;
      }

      if (existing.isExpired) {
        this.dateOverlapWarning =
          `This range overlaps a closed historical version (v${existing.version}, ` +
          `${this.formatDate(existing.effectiveFrom)} – ${this.formatDate(existing.effectiveTo!)}). ` +
          `Past records are sealed for audit — choose a later date.`;
        return;
      }

      if (existing.isFuture) {
        this.dateOverlapWarning =
          `This overlaps an already scheduled version v${existing.version} starting ` +
          `${this.formatDate(existing.effectiveFrom)}. Adjust the dates or edit that scheduled entry first.`;
        return;
      }
    }
  }

  submitEncashmentRule(): void {
    this.checkDateOverlap();
    this.checkMonthSlabOverlap();
    this.checkPrincipalSlabOverlap();

    if (
      this.matrixForm.invalid ||
      this.monthSlabWarning ||
      this.principalSlabWarning ||
      this.dateOverlapWarning ||
      !this.activeTenure
    ) {
      this.matrixForm.markAllAsTouched();
      return;
    }

    const formValue = this.matrixForm.getRawValue();
    const nextVersion = this.activeTenure.versions.length + 1;
    const willAutoClose = !!this.supersedeNotice;

    Swal.fire({
      title: 'Save new encashment rule version?',
      html:
        `This creates <strong>version v${nextVersion}</strong> for ` +
        `<strong>${this.activeTenure.durationName}</strong>, effective ${this.formatDate(formValue.effectiveFrom)}.<br><br>` +
        (willAutoClose ? `<span style="color:#a87d1f;">${this.supersedeNotice}</span><br><br>` : '') +
        `Premature encashments already settled under the previous version keep their original rule-set — this never rewrites history.`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#0f2240',
      confirmButtonText: 'Yes, save version'
    }).then(result => {
      if (!result.isConfirmed) return;
      this.submitting = true;

      setTimeout(() => {
        if (this.activeTenure) {
          const newVersion: EncashmentVersion = {
            id: Date.now(),
            tenorId: this.activeTenure.id,
            effectiveFrom: formValue.effectiveFrom,
            effectiveTo: formValue.hasEndDate ? formValue.effectiveTo : null,
            reasonForChange: formValue.reasonForChange,
            monthSlabs: (formValue.monthSlabs as any[]).map((s, i) => ({
              id: this.activeTenure!.id * 1000 + i + 1,
              minMonths: +s.minMonths,
              maxMonths: +s.maxMonths,
              baseYieldRate: +s.baseYieldRate,
              penaltyRate: +s.penaltyRate
            })),
            principalModifierEnabled: !!formValue.principalModifierEnabled,
            principalModifierSlabs: (formValue.principalModifierSlabs as any[]).map((p, i) => ({
              id: this.activeTenure!.id * 2000 + i + 1,
              principalFrom: +p.principalFrom,
              principalTo: +p.principalTo,
              modifier: +p.modifier
            })),
            segmentOverrides: (formValue.segmentOverrides as any[]).map(s => ({
              segment: s.segment,
              penaltyAdjustment: +s.penaltyAdjustment
            })),
            exceptions: {
              waiveOnDeathClaim: !!formValue.waiveOnDeathClaim,
              capEnabled: !!formValue.capEnabled,
              capValue: +formValue.capValue,
              allowPartialEncashment: !!formValue.allowPartialEncashment
            },
            createdAt: this.todayIso(),
            createdBy: 'You'
          };

          const newEnd = newVersion.effectiveTo || '9999-12-31';

          const adjustedExisting: EncashmentVersion[] = this.activeTenure.versions.map(v => {
            const exEnd = v.effectiveTo || '9999-12-31';
            const overlaps = newVersion.effectiveFrom <= exEnd && v.effectiveFrom <= newEnd;
            const base: EncashmentVersion = {
              id: v.id,
              tenorId: v.tenorId,
              effectiveFrom: v.effectiveFrom,
              effectiveTo: v.effectiveTo,
              reasonForChange: v.reasonForChange,
              monthSlabs: v.monthSlabs,
              principalModifierEnabled: v.principalModifierEnabled,
              principalModifierSlabs: v.principalModifierSlabs,
              segmentOverrides: v.segmentOverrides,
              exceptions: v.exceptions,
              createdAt: v.createdAt,
              createdBy: v.createdBy
            };
            if (overlaps && v.isCurrent) {
              base.effectiveTo = this.previousDay(newVersion.effectiveFrom);
            }
            return base;
          });

          const allVersions = [...adjustedExisting, newVersion];
          const updatedTenure = this.attachEncashmentHistory(this.activeTenure, allVersions);

          const idx = this.tenureList.findIndex(t => t.id === this.activeTenure!.id);
          if (idx !== -1) {
            this.tenureList[idx] = { ...updatedTenure, isExpanded: true };
            this.applyFilters();
          }
        }

        this.shared.ShowAlert('Success', `Encashment rule saved as v${nextVersion}.`, 'success');
        this.submitting = false;
        this.closeEncashModal();
        this.cdr.detectChanges();

        // Mock service call — swap for real persistence wiring.
        const svc: any = this.service;
        if (svc.saveEncashmentMatrix) {
          svc.saveEncashmentMatrix({ tenorId: this.activeTenure?.id, ...formValue }).subscribe?.({
            error: () => console.warn('Real save failed; dummy update succeeded.')
          });
        }
      }, 700);
    });
  }

  // =========================================================================
  // Helpers
  // =========================================================================

  private todayIso(): string {
    return this.datePipe.transform(new Date(), 'yyyy-MM-dd')!;
  }

  private addDays(iso: string, days: number): string {
    const d = new Date(iso + 'T00:00:00');
    d.setDate(d.getDate() + days);
    return this.datePipe.transform(d, 'yyyy-MM-dd')!;
  }

  private previousDay(iso: string): string {
    return this.addDays(iso, -1);
  }

  formatDate(iso: string): string {
    return this.datePipe.transform(iso, 'dd MMM yyyy') || iso;
  }

  formatAmount(value: number): string {
    return value.toLocaleString('en-US');
  }

  durationLabel(v: EncashmentVersionRecord): string {
    const start = new Date(v.effectiveFrom + 'T00:00:00');
    const endIso = v.effectiveTo || this.todayIso();
    const end = new Date(endIso + 'T00:00:00');
    const days = Math.max(0, Math.round((end.getTime() - start.getTime()) / 86400000)) + 1;
    if (!v.effectiveTo) return `${days.toLocaleString('en-US')} days so far`;
    return `${days.toLocaleString('en-US')} days`;
  }

  isPositive(val: number): boolean {
    return val > 0;
  }

  isNegative(val: number): boolean {
    return val < 0;
  }

  trackByTenureId(_i: number, t: TenureWithEncashment): number {
    return t.id;
  }

  trackByProductId(_i: number, p: DpsProduct): number {
    return p.id;
  }

  trackByVersionId(_i: number, v: EncashmentVersionRecord): number {
    return v.id;
  }

  trackByIndex(i: number): number {
    return i;
  }

  goBack(): void {
    this.location.back();
  }

  // =========================================================================
  // MOCK DATA SOURCE
  // -----------------------------------------------------------------------
  // Replace each method below with the matching real API call. The return
  // shape already matches what the rest of the component expects, so no
  // other code needs to change — only the method bodies here.
  // =========================================================================

  private defaultMonthSlabs(): MonthSlab[] {
    return [
      { id: 0, minMonths: 0, maxMonths: 1, baseYieldRate: 3, penaltyRate: 2 },
      { id: 0, minMonths: 1, maxMonths: 3, baseYieldRate: 5, penaltyRate: 1.5 },
      { id: 0, minMonths: 3, maxMonths: 5, baseYieldRate: 6.5, penaltyRate: 1 },
      { id: 0, minMonths: 5, maxMonths: 6, baseYieldRate: 7.5, penaltyRate: 0.5 }
    ];
  }

  /** Swap for: this.service.getDpsProducts() */
  private getDummyProducts(): DpsProduct[] {
    return [
      {
        id: 1,
        name: 'Standard DPS Savings',
        code: 'DPS-STD-001',
        category: 'DPS',
        description: 'Standard monthly Deposit Pension Scheme for everyday savers.',
        depositConfig: {
          type: 'Range',
          currency: 'BDT',
          symbol: '?',
          frequencies: ['Monthly'],
          minAmount: 500,
          maxAmount: 50000,
          step: 500
        }
      },
      {
        id: 2,
        name: 'Millionaire DPS Scheme',
        code: 'DPS-MIL-002',
        category: 'DPS',
        description: 'High-yield scheme for long-term wealth building, paid in fixed tiers.',
        depositConfig: {
          type: 'Fixed',
          currency: 'BDT',
          symbol: '?',
          frequencies: ['Monthly'],
          fixedOptions: [5000, 10000, 25000, 50000]
        }
      },
      {
        id: 3,
        name: 'Flexi DPS Plus',
        code: 'DPS-FLX-003',
        category: 'DPS',
        description: 'Flexible-frequency Deposit Pension Scheme that accepts weekly or fortnightly instalments.',
        depositConfig: {
          type: 'Range',
          currency: 'BDT',
          symbol: '?',
          frequencies: ['Weekly', 'Fortnightly'],
          minAmount: 1000,
          maxAmount: 30000,
          step: 500
        }
      }
    ];
  }

  /** Swap for: this.service.getTenuresForProduct(productId) */
  private getDummyTenures(): TenureDuration[] {
    return [
      { id: 101, productId: 1, code: 'T001', durationName: '6 Months', months: 6, baseRate: 6.5 },
      { id: 102, productId: 1, code: 'T002', durationName: '12 Months', months: 12, baseRate: 7.0 },
      { id: 103, productId: 1, code: 'T005', durationName: '18 Months', months: 18, baseRate: 7.0 },
      { id: 108, productId: 1, code: 'T010', durationName: '9 Months', months: 9, baseRate: 6.0 },
      { id: 104, productId: 2, code: 'T006', durationName: '24 Months', months: 24, baseRate: 7.5 },
      { id: 105, productId: 2, code: 'T007', durationName: '36 Months', months: 36, baseRate: 7.75 },
      { id: 109, productId: 3, code: 'T011', durationName: '12 Months', months: 12, baseRate: 6.8 },
      { id: 110, productId: 3, code: 'T012', durationName: '24 Months', months: 24, baseRate: 7.1 }
    ];
  }

  private defaultExceptions(): EncashmentExceptions {
    return { waiveOnDeathClaim: true, capEnabled: false, capValue: 0, allowPartialEncashment: true };
  }

  /** Builds a chronological encashment-version series for one tenure. */
  private series(
    tenorId: number,
    entries: Array<{
      from: string;
      to: string | null;
      reason: string;
      by: string;
      months: Array<{ min: number; max: number; yield: number; penalty: number }>;
      principalEnabled?: boolean;
      principal?: Array<{ from: number; to: number; mod: number }>;
      segments?: Partial<Record<StandardSegment, number>>;
      exceptions?: Partial<EncashmentExceptions>;
    }>
  ): EncashmentVersion[] {
    return entries.map((e, i) => ({
      id: tenorId * 100 + i + 1,
      tenorId,
      effectiveFrom: e.from,
      effectiveTo: e.to,
      reasonForChange: e.reason,
      createdAt: e.from,
      createdBy: e.by,
      monthSlabs: e.months.map((m, j) => ({
        id: tenorId * 1000 + i * 10 + j + 1,
        minMonths: m.min,
        maxMonths: m.max,
        baseYieldRate: m.yield,
        penaltyRate: m.penalty
      })),
      principalModifierEnabled: !!e.principalEnabled,
      principalModifierSlabs: (e.principal || []).map((p, j) => ({
        id: tenorId * 2000 + i * 10 + j + 1,
        principalFrom: p.from,
        principalTo: p.to,
        modifier: p.mod
      })),
      segmentOverrides: this.standardSegments.map(seg => ({
        segment: seg,
        penaltyAdjustment: e.segments?.[seg as StandardSegment] ?? 0
      })),
      exceptions: { ...this.defaultExceptions(), ...(e.exceptions || {}) }
    }));
  }

  /** Swap for: this.service.getEncashmentVersions(productId) or per-tenure fetch */
  private getDummyEncashmentVersions(): EncashmentVersion[] {
    return [
      ...this.series(101, [
        {
          from: '2024-01-01', to: '2025-12-31', reason: 'Initial encashment matrix.', by: 'S. Rahman',
          months: [
            { min: 0, max: 1, yield: 2.5, penalty: 2.5 },
            { min: 1, max: 3, yield: 4.5, penalty: 2 },
            { min: 3, max: 6, yield: 6, penalty: 1 }
          ]
        },
        {
          from: '2026-01-01', to: null, reason: 'Yearly review — added senior-citizen and staff overrides.', by: 'N. Chowdhury',
          months: [
            { min: 0, max: 1, yield: 3, penalty: 2 },
            { min: 1, max: 3, yield: 5, penalty: 1.5 },
            { min: 3, max: 5, yield: 6.5, penalty: 1 },
            { min: 5, max: 6, yield: 7.5, penalty: 0.5 }
          ],
          principalEnabled: true,
          principal: [
            { from: 0, to: 100000, mod: 0 },
            { from: 100000, to: 1000000, mod: 0.25 },
            { from: 1000000, to: 99999999, mod: 0.5 }
          ],
          segments: { 'Senior citizen': -0.25, 'Staff / employee': -2, 'Premium / HNI': -0.1 },
          exceptions: { capEnabled: true, capValue: 2 }
        }
      ]),
      ...this.series(102, [
        {
          from: '2026-01-01', to: null, reason: 'Initial encashment matrix for the new tenure.', by: 'N. Chowdhury',
          months: [
            { min: 0, max: 2, yield: 3, penalty: 2.5 },
            { min: 2, max: 6, yield: 5.5, penalty: 1.5 },
            { min: 6, max: 12, yield: 7, penalty: 0.5 }
          ],
          segments: { 'Senior citizen': -0.2 }
        }
      ]),
      ...this.series(103, [
        {
          from: '2026-01-01', to: null, reason: 'Initial encashment matrix for the new tenure.', by: 'N. Chowdhury',
          months: [
            { min: 0, max: 3, yield: 3, penalty: 3 },
            { min: 3, max: 9, yield: 5.5, penalty: 1.5 },
            { min: 9, max: 18, yield: 7, penalty: 0.5 }
          ]
        }
      ]),
      // Tenure 108 deliberately gets none — exercises the empty state.
      ...this.series(104, [
        {
          from: '2026-01-01', to: null, reason: 'Annual treasury revision — high-tier scheme launch matrix.', by: 'A. Karim',
          months: [
            { min: 0, max: 3, yield: 3.5, penalty: 3 },
            { min: 3, max: 12, yield: 6, penalty: 1.5 },
            { min: 12, max: 24, yield: 7.5, penalty: 0.5 }
          ],
          principalEnabled: true,
          principal: [
            { from: 0, to: 25000, mod: 0 },
            { from: 25000, to: 50000, mod: 0.15 }
          ],
          segments: { 'Premium / HNI': -0.3 }
        }
      ])
      // Tenure 105, 109, 110 deliberately get none — more empty-state examples.
    ];
  }
}













the above code is my front end code, to make you understand how the bussiness logic i have designed, let me tell you in brief how this page should be worked, 
when the user creates a new product then he set up tenure wise interest rate and this rate we configure as base tenure , adn this base tenure have a EffectiveFrom and LastEffectiveTo date , i will take the current rate only who is not expired yet for configuring premature rate , a product may have multiple active tenure rate , and in one tenure they have premaure rate configuration, there will be one active configuration under one tenure , some may scheduled for later some may expired , but the current actice premature rate will be one only , please check the above front end codo to understand how this is working , when set the effective from date then check the current bussiness date businessDate, which is coming from the service 


here is the table below who is working for this premature configuration page making synamicm 



using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("PRODUCT_BUILDER")]
    public class ProductBuilders : BaseEntity, IAuditableEntity
    {

        [Column("NAME")]
        public string? Name { get; set; }

        [Column("CODE")]
        public string? Code { get; set; }

        [Column("DESCRIPTION")]
        public string? Description { get; set; }
        [Column("VERSION")]
        public string? Version { get; set; }
        public bool? status { get; set; }

        [Column("DAY_SIZE_ID")]
        public int? DaySizeId { get; set; }
        public DaySize? DaySize { get; set; }

        [Column("INTEREST_CALCULATION_FREQUENCY_ID")]
        public int? InterestCalculationFrequencyId { get; set; }
        public InterestCalculationFrequency? InterestCalculationFrequency { get; set; }

        [Column("ACCURED_ID")]
        public int? AccuredId { get; set; }
        public Accured? Accured { get; set; }

        [Column("INTEREST_CALCULATION_TYPE_ID")]
        public int? InterestCalculationTypeId { get; set; }
        public InterestCalculationType? InterestCalculationType { get; set; }
        [Column("INTEREST_RATE_MIN")]
        public decimal? InterestRateMin { get; set; }

        [Column("INTEREST_RATE_MAX")]
        public decimal? InterestRateMax { get; set; }
        public decimal? interestRate { get; set; }
        [Column("BANK_RATE")]
        public decimal? bankRate { get; set; }

        [Column("CAPITALIZATION_RULE_ID")]
        public int? CapitalizationRuleId { get; set; }
        public InterestCapitalizeCalculationRule? CapitalizationRule { get; set; }
        [Column("CAPITALIZATION_FREQUENCY_ID")]
        public int? capitalizationFrequencyId { get; set; }
        public CapitalizeFrequency? capitalizationFrequency { get; set; }
        [Column("CURRENCY_ID")]
        public int? currencyId { get; set; }
        public Country? currency { get; set; }

        [Column("BALANCE_TYPE_ID")]
        public int? BalanceTypeId { get; set; }
        public BalanceType? BalanceType { get; set; }

        [Column("TAXKEY_ID")]
        public int? taxKeyId { get; set; }
        public TaxKey? taxKey { get; set; }

        [Column("EXCISEDUTY_ID")]
        public int? exciseDutyId { get; set; }
        public ExciseDuty? exciseDuty { get; set; }
        [Column("INPUTOR_ID")]
        public string? inputorId { get; set; }
        //public ApplicationUser? inputor { get; set; }
        [Column("AUTHORIZER_ID")]
        public string? authorizerId { get; set; }
        //public ApplicationUser? authorizer { get; set; }
        public int? accountGroupId { get; set; }
        //public AccountGroup? accountGroup { get; set; }
        //
        public int? ledgerId { get; set; }
        //public Ledger? ledger { get; set; }

        public int? plInterestLedgerId { get; set; }
        //public Ledger? plInterestLedger { get; set; }

        public int? interestProvisionLedgerId { get; set; }
        //public Ledger? interestProvisionLedger { get; set; }
        //
        [Column("PRODUCT_CATEGORY_ID")]
        public int? productCategoryId { get; set; }
        public ProductCategory? productCategory { get; set; }

        public string categoryName { get; set; }
        public string categoryCode { get; set; }

        public string? ImagePath { get; set; }//Icon

        public string? baseString { get; set; }
        public int? preProductId { get; set; }
        public ProductBuilders? preProduct { get; set; }
        public int? productStatusId { get; set; }
        public LoanProductStatus? productStatus { get; set; }

        public int? durationId { get; set; }
        public Duration? duration { get; set; }
        public decimal? minAmount { get; set; }
        public decimal? maxAmount { get; set; }
        public DateTime? effectiveDate { get; set; }
        public DateTime? modificationDate { get; set; }
        public DateTime? productCreateDate { get; set; }

        public int? accountTypeId { get; set; }
        public AccountType? accountType { get; set; }

        [Column("PAYMENT_TYPE_ID")]
        public int? paymentTypeId { get; set; }
        public PaymentType paymentType { get; set; }
        public decimal maxwithdrawalLimit { get; set; }
        public decimal overdraftLimit { get; set; }
        public decimal transactionLimit { get; set; }
        public int transactionfrequencies { get; set; }
        [Column("CURRENCYINFO_ID")]
        public int? currencyInfoId { get; set; }
        public CurrencyInfo? currencyInfo { get; set; }

        public int? lockInPeriodValue { get; set; }
        public string? lockInPeriodUnit { get; set; }
        public string? InstallmentFrequency { get; set; }
        public int? IsFixedAmount { get; set; } // 0 = Range, 1 = Fixed List
        public string ProductGlCode { get; set; }
        public string IntExpenseGlCode { get; set; }
        public string IntPayableGlCode { get; set; }
        public int? IsProductDistributed { get; set; } // 0 = Not Distributed, 1 = Distributed
        public int? IsGLMapped { get; set; } // 0 = Not Distributed, 1 = Distributed
        public bool? IsVoucherConfigured { get; set; } = false;

    }
}




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    public class DpsTenure:BaseEntity
    {
        public int? productBuilderId { get; set; }
        public ProductBuilders productBuilder { get; set; }
        public int? durationId { get; set; }
        public Duration duration { get; set; }
        public decimal? InterestRate { get; set; }



        public bool? IsActive { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? LastEffectiveTo { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    public class PrematureEncashmentRate:BaseEntity
    {
        public decimal? EncashRate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime? InActivedate { get; set; }
        public bool? IsActive { get; set; }
        public int? ProductId { get; set; }
        public ProductBuilders Product { get; set; }
        public int? TenureId { get; set; }
        public DpsTenure Tenure { get; set; }

        public int? TenureRateConfigId { get; set; }
        public DpsTenureRateConfig TenureRateConfig { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ReasonForChange { get; set; }
        //Exceptions and regulatory caps
        public bool? IsDeathClaimPenaltyWaived { get; set; }
        public bool? IsPenaltyCapEnabled { get; set; }
        public decimal? MaximumPenaltyRate { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    public class PreMatureEncashTenureSlab : BaseEntity
    {
        public int? PrematureEncashmentRateId { get; set; }
        public PrematureEncashmentRate PrematureEncashmentRate { get; set; }

        // Time Held range (e.g., Min = 0 Months, Max = 3 Months)
        public int? CompletedMonthsMin { get; set; }
        public int? CompletedMonthsMax { get; set; }

        // Manually Entered Yield Rate (e.g., 2.50%)
        public decimal? AppliedRate { get; set; } = decimal.Zero;
        public decimal? BaseYield { get; set; } = decimal.Zero;
        public decimal? Penalty { get; set; } = decimal.Zero;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime? InActivedate { get; set; }
        public bool? IsActive { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    public class PrematureEncashCustomerTypeSlab:BaseEntity
    {
        public int? PrematureEncashmentRateId { get; set; }
        public PrematureEncashmentRate PrematureEncashmentRate { get; set; }
        public string CustomerTypeName { get; set; }
        public decimal? PercentagePenalty { get; set; } = decimal.Zero;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime? InactivatedDate { get; set; }
        public bool? IsActive { get; set; } = false;
    }
}



and you are allowed only use this controller PrematureEncashRateController and this service PrematureEncashRateService, do not use any palce to write code except these controller and service 
so make the relevant api and service to make my front end completely dynamic, and keep version as like the front end in the controller , so i want to make every part of this front end dynamic, 

