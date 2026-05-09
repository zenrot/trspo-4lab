import { IEntityBase } from "./entity-base";

export enum TestRunState {
    NotScheduled = 0,
    Scheduled = 1,
    Completed = 2
}

export interface ITestRun extends IEntityBase {
    courseLabTestMappingId: number;
    studentLabSubmissionId: number;
    state: TestRunState;
    scheduledDate: string;
    success: boolean | undefined;
    message: string | undefined;
}

export class TestRun implements ITestRun {
    id: number | undefined;

    courseLabTestMappingId: number;
    studentLabSubmissionId: number;
    state: TestRunState;
    scheduledDate: string;
    parsedScheduledDate: number;
    success: boolean | undefined;
    message: string | undefined;

    constructor(raw: ITestRun) {
        this.id = raw.id;

        this.courseLabTestMappingId = raw.courseLabTestMappingId;
        this.studentLabSubmissionId = raw.studentLabSubmissionId;
        this.state = raw.state;
        this.scheduledDate = raw.scheduledDate;
        this.parsedScheduledDate = Date.parse(this.scheduledDate);
        this.success = raw.success;
        this.message = raw.message;
    }
}
