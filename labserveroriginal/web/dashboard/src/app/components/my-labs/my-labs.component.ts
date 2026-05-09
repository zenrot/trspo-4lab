import { Component, OnInit, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ApiService } from '../../services/api.service';
import { IStudent } from '../../models/entity/student';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { cStudentLabStatus, IStudentLab } from '../../models/entity/student-lab';
import { DatePipe, NgClass } from '@angular/common';
import { LabNotesModalComponent } from './lab-notes-modal/lab-notes-modal.component';
import { GitLabHelperService } from '../../services/git-lab-helper.service';
import { IStudentLabSubmission } from '../../models/entity/student-lab-submission';
import { ITestRun } from '../../models/entity/test-run';

@Component({
  selector: 'app-my-labs',
  standalone: true,
  imports: [TranslateModule, DatePipe, NgClass],
  templateUrl: './my-labs.component.html',
  styleUrl: './my-labs.component.css'
})
export class MyLabsComponent implements OnInit {
  protected mydata?: IStudent;
  
  gitLabCredsModalRef?: BsModalRef;
  labNotesModalRef?: BsModalRef;

  constructor(private route: ActivatedRoute,
    private apiService: ApiService,
    private modalService: BsModalService) {}
  
  async ngOnInit() {
    this.route.paramMap.subscribe(async params => {
      const token = params.get("token");
      if (token) {
        this.mydata = await this.apiService.getData(token);
        console.log(this.mydata);
      }
    });
  }

  protected openGitLabCredsModal(template: TemplateRef<void>) {
    this.gitLabCredsModalRef = this.modalService.show(template);
  }

  protected getLabs(): IStudentLab[] {
    if (this.mydata) {
      return this.mydata.labsData;
    } else {
      return [];
    }
  }

  protected makeGitLabLink(lab: IStudentLab): string {
    if (lab.gitLabProject) {
      return GitLabHelperService.fromUrl(lab.gitLabProject.web_url);
    } else {
      return "";
    }
  }

  protected getGitLabUrl(): string {
    return GitLabHelperService.gitLabUrlBase;
  }

  protected getLabName(studentLab: IStudentLab): string {
    return studentLab.groupCourseLab?.courseLab?.name ?? 'NOT FOUND';
  }

  protected getOpenedDate(studentLab: IStudentLab): number {
    return Date.parse(studentLab.openedDate!);
  }

  protected getPlagiarismPercent(lab: IStudentLab): number | undefined {
    const submission = this.getLatestSubmission(lab);
    if (!submission) {
      return undefined;
    }

    const run = this.getLatestPlagiarismRun(submission);
    return this.parsePlagiarismPercent(run?.message);
  }

  protected getPlagiarismText(lab: IStudentLab): string {
    const percent = this.getPlagiarismPercent(lab);
    if (percent === undefined) {
      return 'labsTablePlagiarismPending';
    }

    return `${percent.toFixed(1).replace('.', ',')} %`;
  }

  protected getPlagiarismBadgeClass(lab: IStudentLab): string {
    const percent = this.getPlagiarismPercent(lab);
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

  protected getPlagiarismProgressClass(lab: IStudentLab): string {
    const percent = this.getPlagiarismPercent(lab);
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

  private getLatestSubmission(lab: IStudentLab): IStudentLabSubmission | undefined {
    return [...(lab.labSubmissions ?? [])]
      .sort((first, second) => Date.parse(second.submittedDate) - Date.parse(first.submittedDate))[0];
  }

  private getLatestPlagiarismRun(submission: IStudentLabSubmission): ITestRun | undefined {
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

  protected getStatusBgColor(status: cStudentLabStatus): string {
    switch (status) {
      case cStudentLabStatus.cCompleted:
        return 'btn btn-success text-white tw-bold';
      case cStudentLabStatus.cOverdue:
        return 'btn btn-danger text-white tw-bold';
      case cStudentLabStatus.cInProgress:
        return 'btn btn-secondary text-white tw-bold';
    }
  }

  protected getStatusTranslateSuffix(status: cStudentLabStatus): string {
    switch (status) {
      case cStudentLabStatus.cCompleted:
        return 'Complete';
      case cStudentLabStatus.cOverdue:
        return 'Overdue';
      case cStudentLabStatus.cInProgress:
        return 'InProgress';
    }
  }

  protected openNotesModal(notesText: string) {
    const initialState: ModalOptions = {
      initialState: {
        notes: notesText
      }
    };
    this.labNotesModalRef = this.modalService.show(LabNotesModalComponent, initialState);
  }
}
