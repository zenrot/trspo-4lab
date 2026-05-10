import { Injectable } from '@angular/core';
import { ApiServiceBase } from './api-base.service';
import { ILabTest } from '../../models/entity/lab-test';

@Injectable({
  providedIn: 'root'
})
export class CourseLabTestsService extends ApiServiceBase {

  constructor() {
    super("rest/Courses");
  }

  private makeEndpoint(courseId: number, labId: number, suffix: string = ''): string {
    return `/${courseId}/labs/${labId}/tests${suffix}`;
  }

  public async getAll(courseId: number, labId: number): Promise<ILabTest[]> {
    return await this.apiGet<ILabTest[]>(this.makeEndpoint(courseId, labId));
  }

  public async assign(courseId: number, labId: number, testId: number): Promise<ILabTest> {
    return await this.apiPost<ILabTest>(this.makeEndpoint(courseId, labId), {
      testId,
      courseLabId: labId,
      activated: true
    });
  }

  public async setActivated(courseId: number, labId: number, testId: number, activated: boolean): Promise<boolean> {
    return await this.apiPut<boolean>(this.makeEndpoint(courseId, labId, `/${testId}`), { activated });
  }
}
