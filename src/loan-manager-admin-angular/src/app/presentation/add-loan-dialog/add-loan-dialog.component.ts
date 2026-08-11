import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Customer } from '../../domain/entities/customer.entity';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { CreateLoanUseCase } from '../../application/use-cases/create-loan.use-case';

@Component({
  selector: 'lm-add-loan-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './add-loan-dialog.component.html',
  styleUrls: ['./add-loan-dialog.component.scss'],
})
export class AddLoanDialogComponent implements OnInit {
  submitting = false;
  customers: Customer[] = [];

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    customerId: ['', Validators.required],
    principal: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddLoanDialogComponent>,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly createLoan: CreateLoanUseCase,
  ) {}

  ngOnInit(): void {
    this.getCustomers.execute().subscribe((customers) => (this.customers = customers));
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { customerId, principal } = this.form.getRawValue();
    this.createLoan.execute(customerId!, principal!).subscribe(() => {
      this.dialogRef.close({ added: true });
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
