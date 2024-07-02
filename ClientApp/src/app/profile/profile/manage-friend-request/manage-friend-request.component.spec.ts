import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageFriendRequestComponent } from './manage-friend-request.component';

describe('ManageFriendRequestComponent', () => {
  let component: ManageFriendRequestComponent;
  let fixture: ComponentFixture<ManageFriendRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ManageFriendRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ManageFriendRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
