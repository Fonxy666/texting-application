import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GenerateAvatarChangeRequestComponent } from './generate-avatar-change-request.component';

describe('GenerateAvatarChangeRequestComponent', () => {
  let component: GenerateAvatarChangeRequestComponent;
  let fixture: ComponentFixture<GenerateAvatarChangeRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GenerateAvatarChangeRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GenerateAvatarChangeRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
