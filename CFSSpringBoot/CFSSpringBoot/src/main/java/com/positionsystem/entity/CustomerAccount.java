package com.positionsystem.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "custfile")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class CustomerAccount {
    
    @Id
    @Column(name = "acct_no", length = 50)
    private String accountNo;
    
    @Column(name = "cust_name1", nullable = false, length = 255)
    private String customerName;
    
    @Column(name = "abbrv_name", length = 100)
    private String abbreviatedName;
}
