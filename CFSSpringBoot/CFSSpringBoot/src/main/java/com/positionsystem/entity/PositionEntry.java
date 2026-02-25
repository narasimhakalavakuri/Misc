package com.positionsystem.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@Table(name = "tbl_posentry")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class PositionEntry {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, unique = true, length = 100)
    private String uid;
    
    @Column(name = "entry_date", nullable = false)
    private LocalDate entryDate;
    
    @Column(name = "value_date", nullable = false)
    private LocalDate valueDate;
    
    @Column(nullable = false, length = 50)
    private String department;
    
    @Column(name = "transaction_type", nullable = false, length = 50)
    private String transactionType;
    
    @Column(length = 200)
    private String reference;
    
    @Column(name = "their_reference", length = 200)
    private String theirReference;
    
    @Column(name = "inward_currency", nullable = false, length = 3)
    private String inwardCurrency;
    
    @Column(name = "inward_amount", nullable = false, precision = 18, scale = 2)
    private BigDecimal inwardAmount;
    
    @Column(name = "outward_currency", nullable = false, length = 3)
    private String outwardCurrency;
    
    @Column(name = "outward_amount", nullable = false, precision = 18, scale = 2)
    private BigDecimal outwardAmount;
    
    @Column(name = "exchange_rate", nullable = false, precision = 18, scale = 6)
    private BigDecimal exchangeRate;
    
    @Column(name = "calc_operator", length = 1)
    private String calcOperator;
    
    @Column(name = "inward_account", nullable = false, length = 50)
    private String inwardAccount;
    
    @Column(name = "inward_account_name", length = 255)
    private String inwardAccountName;
    
    @Column(name = "outward_account", nullable = false, length = 50)
    private String outwardAccount;
    
    @Column(name = "outward_account_name", length = 255)
    private String outwardAccountName;
    
    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 20)
    private EntryStatus status;
    
    @Column(name = "checked_out_by", length = 255)
    private String checkedOutBy;
    
    @Column(name = "approved_date")
    private LocalDateTime approvedDate;
    
    @Column(name = "created_by", nullable = false, length = 255)
    private String createdBy;
    
    @Column(name = "created_date", nullable = false, updatable = false)
    private LocalDateTime createdDate;
    
    @Column(name = "modified_by", length = 255)
    private String modifiedBy;
    
    @Column(name = "modified_date")
    private LocalDateTime modifiedDate;
    
    @Column(name = "is_fe_exchange")
    private Boolean isFeExchange = false;
    
    @PrePersist
    protected void onCreate() {
        createdDate = LocalDateTime.now();
        if (status == null) {
            status = EntryStatus.PENDING;
        }
    }
    
    @PreUpdate
    protected void onUpdate() {
        modifiedDate = LocalDateTime.now();
    }
}