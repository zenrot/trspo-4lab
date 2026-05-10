import { IEntityBase } from "./entity-base";
import { ITest } from "./test";

export interface ILabTest extends IEntityBase {
    testId: number;
    courseLabId: number;
    activated: boolean;
    test?: ITest;
}
