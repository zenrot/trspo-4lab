import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ILabTest } from '../../../../../../models/entity/lab-test';
import { ITest } from '../../../../../../models/entity/test';
import { ICourseLab } from '../../../../../../models/entity/course-lab';
import { CourseLabTestsService } from '../../../../../../services/API/course-lab-tests.service';
import { TestService } from '../../../../../../services/API/test.service';
import { CourseLabsService } from '../../../../../../services/API/course-labs.service';
import { CoursesService } from '../../../../../../services/API/courses.service';
import { ICourse } from '../../../../../../models/entity/course';

@Component({
  selector: 'app-course-lab-tests-page',
  standalone: true,
  imports: [TranslateModule, RouterLink],
  templateUrl: './course-lab-tests-page.component.html',
  styleUrl: './course-lab-tests-page.component.css'
})
export class CourseLabTestsPageComponent implements OnInit {
  protected courseId?: number;
  protected labId?: number;
  protected course?: ICourse;
  protected lab?: ICourseLab;

  protected labTests: ILabTest[] = [];
  protected allTests: ITest[] = [];

  constructor(
    private route: ActivatedRoute,
    private labTestsService: CourseLabTestsService,
    private testService: TestService,
    private labsService: CourseLabsService,
    private coursesService: CoursesService
  ) {}

  async ngOnInit() {
    this.route.paramMap.subscribe(async params => {
      const courseIdParam = params.get('courseId');
      const labIdParam = params.get('labId');
      if (courseIdParam && labIdParam) {
        this.courseId = +courseIdParam;
        this.labId = +labIdParam;
        await this.loadData();
      }
    });
  }

  private async loadData() {
    if (!this.courseId || !this.labId) return;

    [this.course, this.allTests, this.labTests] = await Promise.all([
      this.coursesService.getById(this.courseId),
      this.testService.getAll(),
      this.labTestsService.getAll(this.courseId, this.labId)
    ]);

    const labs = await this.labsService.getAll(this.courseId);
    this.lab = labs.find(l => l.id === this.labId);
  }

  protected getTestName(testId: number): string {
    return this.allTests.find(t => t.id === testId)?.name ?? String(testId);
  }

  protected isAssigned(testId: number): boolean {
    return this.labTests.some(lt => lt.testId === testId);
  }

  protected isActivated(testId: number): boolean {
    return this.labTests.find(lt => lt.testId === testId)?.activated ?? false;
  }

  protected async assignTest(testId: number) {
    if (!this.courseId || !this.labId) return;
    const result = await this.labTestsService.assign(this.courseId, this.labId, testId);
    if (result) {
      this.labTests = await this.labTestsService.getAll(this.courseId, this.labId);
    }
  }

  protected async toggleActivated(testId: number) {
    if (!this.courseId || !this.labId) return;
    const current = this.isActivated(testId);
    const result = await this.labTestsService.setActivated(this.courseId, this.labId, testId, !current);
    if (result !== null) {
      this.labTests = await this.labTestsService.getAll(this.courseId, this.labId);
    }
  }
}
