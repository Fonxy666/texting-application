import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GenerateUserKeyChangeRequestComponent } from './generate-user-key-change-request.component';

describe('GenerateUserKeyChangeRequestComponent', () => {
  let component: GenerateUserKeyChangeRequestComponent;
  let fixture: ComponentFixture<GenerateUserKeyChangeRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GenerateUserKeyChangeRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GenerateUserKeyChangeRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
