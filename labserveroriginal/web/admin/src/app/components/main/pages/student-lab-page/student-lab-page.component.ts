import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { StudentService } from '../../../../services/API/student.service';
import { cStudentLabStatus, StudentLab } from '../../../../models/entity/student-lab';
import { cDataConversionOption } from '../../../../services/API/api-base.service';
import { StudentLabSubmission } from '../../../../models/entity/student-lab-submission';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ITestRun } from '../../../../models/entity/test-run';

@Component({
  selector: 'app-student-lab-page',
  standalone: true,
  imports: [TranslateModule, DatePipe, NgClass, FormsModule, RouterLink],
  templateUrl: './student-lab-page.component.html',
  styleUrl: './student-lab-page.component.css'
})
export class StudentLabPageComponent implements OnInit, OnDestroy {
  protected studentId?: number;
  protected studentLabId?: number;

  protected studentLab?: StudentLab;

  constructor(private route: ActivatedRoute,
    private studentService: StudentService) {}
  
  async ngOnInit() {
    this.route.paramMap.subscribe(async params => {
      const labId = params.get("labId");
      const studentId = params.get("studentId");

      if (labId && studentId) {
        this.studentLabId = +labId;
        this.studentId = +studentId;

        const rawLab = await this.studentService.getLabById(this.studentId, this.studentLabId, cDataConversionOption.Full);
        this.studentLab = new StudentLab(rawLab);
      }
    })  
  }

  async ngOnDestroy() {
  }

  protected getLabSubmissions(): StudentLabSubmission[] {
    if (this.studentLab && this.studentLab.labSubmissions) {
      return this.studentLab.labSubmissions.sort((f, s) => f.parsedSubmittedDate < s.parsedSubmittedDate ? 1 : -1);
    }
    return [];
  }

  protected getPlagiarismPercent(submission: StudentLabSubmission): number | undefined {
    const run = this.getLatestPlagiarismRun(submission);
    return this.parsePlagiarismPercent(run?.message);
  }

  protected getPlagiarismText(submission: StudentLabSubmission): string {
    const percent = this.getPlagiarismPercent(submission);
    if (percent === undefined) {
      return 'studentLabPagePlagiarismPending';
    }

    return `${percent.toFixed(1).replace('.', ',')} %`;
  }

  protected getPlagiarismBadgeClass(submission: StudentLabSubmission): string {
    const percent = this.getPlagiarismPercent(submission);
    if (percent === undefined) {
      return 'badge text-bg-secondary';
    }
    if (percent >= 75) {
      return 'badge text-bg-danger';
    }
    if (percent >= 50) {
      return 'badge text-bg-warning text-dark';
    }

    return 'badge text-bg-success';
  }

  protected getPlagiarismProgressClass(submission: StudentLabSubmission): string {
    const percent = this.getPlagiarismPercent(submission);
    if (percent === undefined) {
      return 'progress-bar bg-secondary';
    }
    if (percent >= 75) {
      return 'progress-bar bg-danger';
    }
    if (percent >= 50) {
      return 'progress-bar bg-warning';
    }

    return 'progress-bar bg-success';
  }

  private getLatestPlagiarismRun(submission: StudentLabSubmission): ITestRun | undefined {
    return [...(submission.testRuns ?? [])]
      .filter(run => this.parsePlagiarismPercent(run.message) !== undefined)
      .sort((first, second) => Date.parse(second.scheduledDate) - Date.parse(first.scheduledDate))[0];
  }

  private parsePlagiarismPercent(message: string | undefined): number | undefined {
    if (!message) {
      return undefined;
    }

    const match = message.match(/Similarity\s*=\s*(\d+(?:[,.]\d+)?)\s*%/i);
    if (!match) {
      return undefined;
    }

    const percent = Number.parseFloat(match[1].replace(',', '.'));
    if (!Number.isFinite(percent)) {
      return undefined;
    }

    return Math.max(0, Math.min(100, percent));
  }

  protected getLabStatuses(): cStudentLabStatus[] {
    return [cStudentLabStatus.cInProgress, cStudentLabStatus.cOverdue, cStudentLabStatus.cCompleted];
  }

  protected getStatusTranslationSuffix(status: cStudentLabStatus): string {
    switch (status) {
      case cStudentLabStatus.cInProgress:
        return 'InProgress';
      case cStudentLabStatus.cOverdue:
        return 'Overdue';
      case cStudentLabStatus.cCompleted:
        return 'Completed';
      default:
        return 'UnkownStatus';
    }
  }

  protected getStatusBgColor(status: cStudentLabStatus): string {
    if (this.studentLab && this.studentLab.status === status) {
      switch (status) {
        case cStudentLabStatus.cInProgress:
        return 'bg-primary text-white fw-bold';
      case cStudentLabStatus.cOverdue:
        return 'bg-danger text-white fw-bold';
      case cStudentLabStatus.cCompleted:
        return 'bg-success text-white fw-bold';
      default:
        return '';
      }
    } else {
      return "";
    }
  }

  protected setStatus(status: cStudentLabStatus) {
    if (this.studentLab) {
      this.studentLab.status = status;
    }
  }

  protected async saveLabData() {
    if (this.studentId && this.studentLabId && this.studentLab) {
      await this.studentService.updateLabById(this.studentId, this.studentLabId, this.studentLab)
    }
  }
}
